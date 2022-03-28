using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 10)]
    public class WorldGrain : Grain, IWorldGrain
    {
        public WorldGrain([PersistentState(Constants.PersistenceKeys.WorldStateName,
            Constants.PersistenceKeys.WorldStorageName)]
            IPersistentState<World> world,
            IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub, ILogger<WorldGrain> logger)
        {
            World = world;
            WanderlandHub = wanderlandHub;
            Logger = logger;
        }

        public IPersistentState<World> World { get; }
        public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHub { get; }
        public ILogger<WorldGrain> Logger { get; set; }

        int _worldLifetimeThresholdInMinutes = 5;
        IDisposable _timer;
        private  void ResetTimer()
        {
            _timer?.Dispose();
            _timer = RegisterTimer(async _ =>
            {
                try
                {
                    var started = World.State.Started;
                    var expires = started.AddMinutes(_worldLifetimeThresholdInMinutes);
                    if (DateTime.Now > expires)
                    {
                        await GrainFactory.GetGrain<ICreatorGrain>(Guid.Empty).DestroyWorld(this);
                        _timer?.Dispose();
                    }
                    else
                    {
                        await WanderlandHub.Clients.Group(World.State.Name).WorldAgeUpdated(new WorldAgeUpdatedEventArgs
                        {
                            World = World.State.Name,
                            Age = World.State.Started - DateTime.Now
                        });
                    }
                }
                catch
                {
                    _timer?.Dispose();
                }
            }, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            ResetTimer();
            await base.OnActivateAsync(cancellationToken);
        }

        public Task<bool> IsWorldEmpty()
        {
            var thingsLeft = World.State.Tiles.SelectMany(_ => _.ThingsHere.Where(_ => _.GetType() == typeof(Wanderer))).ToList();
            Logger.LogInformation($"There are {thingsLeft.Count} Wanderers in {World.State.Name}");

            return Task.FromResult(thingsLeft is not {Count: > 1 });
        }

        Task<World> IWorldGrain.GetWorld()
        {
            return Task.FromResult(World.State);
        }

        async Task IWorldGrain.SetTile(Tile tile)
        {
            if(!World.State.Tiles.Any(x => x.Row == tile.Row && x.Column == tile.Column))
            {
                World.State.Tiles.Add(tile);

                string grainKey = $"{World.State.Name}/{tile.Row}/{tile.Column}";
                var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
                await tileGrain.SetTile(tile);
            }
            else
            {
                World.State.Tiles.First(x => x.Row == tile.Row && x.Column == tile.Column).ThingsHere = tile.ThingsHere;
            }
        }

        Task IWorldGrain.SetWorld(World world)
        {
            World.State = world;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var tile in World.State.Tiles)
            {
                var tileGrain = GrainFactory.GetGrain<ITileGrain>($"{tile.World}/{tile.Row}/{tile.Column}");
                tileGrain.Dispose();
            }

            _timer.Dispose();
            base.DeactivateOnIdle();
        }
    }
}

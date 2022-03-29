using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains;

[CollectionAgeLimit(Minutes = 10)]
public class WorldGrain : Grain, IWorldGrain
{
    readonly int _worldLifetimeThresholdInMinutes = 5;
    private readonly IPersistentState<World> _world;
    private readonly IHubContext<WanderlandHub, IWanderlandHubClient> _wanderlandHub;
    private readonly ILogger<WorldGrain> _logger;

    IDisposable _timer;

    public WorldGrain([PersistentState(Constants.PersistenceKeys.WorldStateName,
        Constants.PersistenceKeys.WorldStorageName)]
        IPersistentState<World> world,
        IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub,
        ILogger<WorldGrain> logger)
    {
        _world = world;
        _wanderlandHub = wanderlandHub;
        _logger = logger;
    }

    private void ResetTimer()
    {
        _timer?.Dispose();
        _timer = RegisterTimer(async _ =>
        {
            try
            {
                var started = _world.State.Started;
                var expires = started.AddMinutes(_worldLifetimeThresholdInMinutes);
                if (DateTime.Now > expires)
                {
                    await GrainFactory.GetGrain<ICreatorGrain>(Guid.Empty).DestroyWorld(this);
                    _timer?.Dispose();
                }
                else
                {
                    await _wanderlandHub.Clients.Group(_world.State.Name).WorldAgeUpdated(new WorldAgeUpdatedEventArgs
                    {
                        World = _world.State.Name,
                        Age = _world.State.Started - DateTime.Now
                    });
                }
            }
            catch
            {
                _timer?.Dispose();
            }
        }, null, TimeSpan.FromMilliseconds(1_000), TimeSpan.FromMilliseconds(1_000));
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ResetTimer();
        await base.OnActivateAsync(cancellationToken);
    }

    public Task<bool> IsWorldEmpty()
    {
        var thingsLeft = _world.State.Tiles.SelectMany(_ => _.ThingsHere.Where(_ => _.GetType() == typeof(Wanderer))).ToList();
        return Task.FromResult(thingsLeft is not { Count: > 1 });
    }

    Task<World> IWorldGrain.GetWorld() => Task.FromResult(_world.State);

    async Task IWorldGrain.SetTile(Tile tile)
    {
        if (!_world.State.Tiles.Any(x => x.Row == tile.Row && x.Column == tile.Column))
        {
            _world.State.Tiles.Add(tile);

            string grainKey = $"{_world.State.Name}/{tile.Row}/{tile.Column}";
            var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
            await tileGrain.SetTile(tile);
        }
        else
        {
            _world.State.Tiles.First(x => x.Row == tile.Row && x.Column == tile.Column).ThingsHere = tile.ThingsHere;
        }
    }

    Task IWorldGrain.SetWorld(World world)
    {
        _world.State = world;
        return Task.CompletedTask;
    }
}

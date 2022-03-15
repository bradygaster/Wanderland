using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class WorldGrain : Grain, IWorldGrain
    {
        public WorldGrain([PersistentState(Constants.PersistenceKeys.WorldStateName, 
            Constants.PersistenceKeys.WorldStorageName)]
            IPersistentState<World> world,
            IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub)
        {
            World = world;
            WanderlandHub = wanderlandHub;
        }

        public IPersistentState<World> World { get; }
        public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHub { get; }

        IDisposable _timer;
        private void ResetTimer()
        {
            _timer?.Dispose();
            _timer = RegisterTimer(async _ =>
            {
                try
                {
                    await WanderlandHub.Clients.Group(World.State.Name).WorldAgeUpdated(new WorldAgeUpdatedEventArgs
                    {
                        World = World.State.Name,
                        Age = World.State.Started - DateTime.Now
                    });
                }
                catch
                {
                    _timer?.Dispose();
                }
            }, null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
        }

        public override async Task OnActivateAsync()
        {
            ResetTimer();
            await base.OnActivateAsync();
        }

        public async Task<bool> IsWorldEmpty()
        {
            var wanderersLeft = new List<string>();
            int wandererCount = 0;
            for (int row = 0; row < World.State.Rows; row++)
            {
                for (int col = 0; col < World.State.Columns; col++)
                {
                    string grainKey = $"{World.State.Name}/{row}/{col}";
                    var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
                    var tile = await tileGrain.GetTile();
                    wandererCount += tile.ThingsHere.Where(x => x.GetType() == typeof(Wanderer)).Count();
                    if(tile.ThingsHere.Count > 0)
                    {
                        foreach (var wanderer in tile.ThingsHere)
                        {
                            wanderersLeft.Add(wanderer.Name);
                        }
                    }
                }
            }

            if(wandererCount == 1)
            {
                var winnerGrain = GrainFactory.GetGrain<IMonsterGrain>(wanderersLeft[0], typeof(MonsterGrain).FullName);
                winnerGrain.Dispose();
            }

            return wandererCount <= 1;
        }

        Task<World> IWorldGrain.GetWorld()
        {
            return Task.FromResult(World.State);
        }

        async Task<ITileGrain> IWorldGrain.MakeTile(Tile tile)
        {
            string grainKey = $"{World.State.Name}/{tile.Row}/{tile.Column}";
            var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
            await tileGrain.SetTileInfo(tile);
            return tileGrain;
        }

        Task IWorldGrain.SetWorld(World world)
        {
            World.State = world;
            return Task.CompletedTask;
        }
    }
}

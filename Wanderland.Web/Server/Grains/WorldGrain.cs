using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class WorldGrain : Grain, IWorldGrain
    {
        public WorldGrain([PersistentState(Constants.PersistenceKeys.WorldStateName, Constants.PersistenceKeys.WorldStorageName)]
            IPersistentState<World> world,
            IHubContext<WanderlandHub> wanderlandHub)
        {
            World = world;
            WanderlandHub = wanderlandHub;
        }

        public IPersistentState<World> World { get; }
        public IHubContext<WanderlandHub> WanderlandHub { get; }

        public void Dispose()
        {
            World.State = null;
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
                    wandererCount += tile.WanderersHere.Count;
                    if(tile.WanderersHere.Count > 0)
                    {
                        foreach (var wanderer in tile.WanderersHere)
                        {
                            wanderersLeft.Add(wanderer.Name);
                        }
                    }
                }
            }

            if(wandererCount == 1)
            {
                try
                {
                    var winnerGrain = GrainFactory.GetGrain<IWanderGrain>(wanderersLeft[0], typeof(WandererGrain).FullName);
                    // todo: record their win
                    winnerGrain.Dispose();
                }
                catch
                {
                    // if that failed, it must be a monster grain, so treat it different
                    var winnerGrain = GrainFactory.GetGrain<IMonsterGrain>(wanderersLeft[0], typeof(MonsterGrain).FullName);
                    winnerGrain.Dispose();
                }
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

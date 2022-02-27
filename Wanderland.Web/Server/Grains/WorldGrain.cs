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

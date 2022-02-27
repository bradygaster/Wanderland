using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class WorldGrain : Grain, IWorldGrain
    {
        IPersistentState<World> _world;

        public WorldGrain([PersistentState(Constants.PersistenceKeys.WorldStateName, Constants.PersistenceKeys.WorldStorageName)]
            IPersistentState<World> world)
        {
            _world = world;
        }

        async Task<World> IWorldGrain.GetWorld()
        {
            return _world.State;
        }

        async Task<ITileGrain> IWorldGrain.MakeTile(Tile tile)
        {
            string grainKey = $"{_world.State.Name}/{tile.Row}/{tile.Column}";
            var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
            await tileGrain.SetTileInfo(tile);
            return tileGrain;
        }

        async Task IWorldGrain.SetWorld(World world)
        {
            _world.State = world;
        }
    }
}

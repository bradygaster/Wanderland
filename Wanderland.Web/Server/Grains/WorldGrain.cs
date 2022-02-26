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
            await _world.ReadStateAsync();
            return _world.State;
        }

        async Task<ITileGrain> IWorldGrain.MakeTile(Tile tile)
        {
            ITileGrain tileGrain = GetTileGrain(tile.Coordinate);
            await tileGrain.SetTileInfo(tile);
            return tileGrain;
        }

        async Task IWorldGrain.SetWorld(World world)
        {
            _world.State = world;
            await _world.WriteStateAsync();
        }

        private ITileGrain GetTileGrain(Coordinate coordinate)
        {
            string grainKey = $"{_world.State.Name}/{coordinate.Row}/{coordinate.Column}";
            var tileGrain = base.GrainFactory.GetGrain<ITileGrain>(grainKey);
            return tileGrain;
        }
    }
}

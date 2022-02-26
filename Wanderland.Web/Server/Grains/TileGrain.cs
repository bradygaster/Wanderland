using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class TileGrain : Grain, ITileGrain
    {
        IPersistentState<Tile> _tile;

        public TileGrain([PersistentState(Constants.PersistenceKeys.TileStateName, Constants.PersistenceKeys.TileStorageName)] 
            IPersistentState<Tile> tile)
        {
            _tile = tile;
        }

        async Task<Tile> ITileGrain.GetTile()
        {
            await _tile.ReadStateAsync();
            return _tile.State;
        }

        async Task ITileGrain.SetTileInfo(Tile tile)
        {
            _tile.State = tile;
            await _tile.WriteStateAsync();
        }
    }
}

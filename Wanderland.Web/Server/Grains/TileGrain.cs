using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class TileGrain : Grain, ITileGrain
    {
        IPersistentState<Tile> _tile;
        private readonly ILogger<TileGrain> _logger;

        public TileGrain([PersistentState(Constants.PersistenceKeys.TileStateName, Constants.PersistenceKeys.TileStorageName)] 
            IPersistentState<Tile> tile,
            ILogger<TileGrain> logger)
        {
            _tile = tile;
            _logger = logger;
        }

        async Task ITileGrain.Arrives(IWanderGrain wanderer)
        {
            var wandererName = wanderer.GetPrimaryKeyString();
            if(!_tile.State.WanderersHere.Any(x => x.GetPrimaryKeyString() == wandererName))
            {
                _tile.State.WanderersHere.Add(wanderer);
                _logger.LogInformation($"{wandererName} has wandered into tile {this.GetPrimaryKeyString()}");
            }
        }

        async Task ITileGrain.Leaves(IWanderGrain wanderer)
        {
            var wandererName = wanderer.GetPrimaryKeyString();
            if (_tile.State.WanderersHere.Any(x => x.GetPrimaryKeyString() == wandererName))
            {
                _tile.State.WanderersHere.Remove(wanderer);
                _logger.LogInformation($"{wandererName} has left tile {this.GetPrimaryKeyString()}");
            }
        }

        async Task<Tile> ITileGrain.GetTile()
        {
            return _tile.State;
        }

        async Task ITileGrain.SetTileInfo(Tile tile)
        {
            _tile.State = tile;
        }
    }
}

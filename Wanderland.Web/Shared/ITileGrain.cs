using Orleans;

namespace Wanderland.Web.Shared
{
    public interface ITileGrain : IGrainWithStringKey, IDisposable
    {
        Task SetTileInfo(Tile tile);
        Task<Tile> GetTile();
        Task Arrives(IWanderGrain wanderer);
        Task Leaves(IWanderGrain wanderer);
    }
}

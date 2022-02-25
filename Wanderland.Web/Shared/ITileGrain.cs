using Orleans;

namespace Wanderland.Web.Shared
{
    public interface ITileGrain : IGrainWithStringKey
    {
        Task SetTileInfo(Tile tile);
    }
}

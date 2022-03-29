using Orleans;

namespace Wanderland.Web.Shared;

public interface ITileGrain : IGrainWithStringKey, IAsyncDisposable
{
    Task SetTile(Tile tile);
    Task<Tile> GetTile();
    Task Arrives(Thing thing);
    Task Leaves(Thing thing);
}

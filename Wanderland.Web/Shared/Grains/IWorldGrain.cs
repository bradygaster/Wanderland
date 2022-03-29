using Orleans;

namespace Wanderland.Web.Shared;

public interface IWorldGrain : IGrainWithStringKey, IAsyncDisposable
{
    Task<World> GetWorld();
    Task SetTile(Tile tile);
    Task SetWorld(World world);
    Task<bool> IsWorldEmpty();
}

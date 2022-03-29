using Orleans;

namespace Wanderland.Web.Shared;

public interface IWorldGrain : IGrainWithStringKey, IDestroyableGrain
{
    Task<World> GetWorld();
    Task SetTile(Tile tile);
    Task SetWorld(World world);
    Task<bool> IsWorldEmpty();
}

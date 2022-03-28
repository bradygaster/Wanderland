using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWorldGrain : IGrainWithStringKey, IDisposable
    {
        Task<World> GetWorld();
        Task SetTile(Tile tile);
        Task SetWorld(World world);
        Task<bool> IsWorldEmpty();
    }
}

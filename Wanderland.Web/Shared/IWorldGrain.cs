using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWorldGrain : IGrainWithStringKey, IDisposable
    {
        Task<World> GetWorld();
        Task<ITileGrain> MakeTile(Tile tile);
        Task SetWorld(World world);
        Task<bool> IsWorldEmpty();
    }
}

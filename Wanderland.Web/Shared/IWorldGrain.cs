using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWorldGrain : IGrainWithStringKey
    {
        Task<World> GetWorld();
        Task<ITileGrain> MakeTile(Tile tile);
        Task SetWorld(World world);
    }
}

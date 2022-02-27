using Orleans;

namespace Wanderland.Web.Shared
{
    public interface ICreatorGrain : IGrainWithGuidKey
    {
        Task<IWorldGrain?> CreateWorld(World world);
        Task<List<World>> GetWorlds();
        Task<bool> WorldExists(string name);
    }
}

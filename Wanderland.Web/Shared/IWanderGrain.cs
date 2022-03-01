using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWanderGrain : IGrainWithStringKey
    {
        Task Wander();
        Task SetLocation(ITileGrain tileGrain);
        Task<Wanderer> GetWanderer();
        Task SetInfo(Wanderer wanderer);
    }
}

using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWanderGrain : IGrainWithStringKey, IDisposable
    {
        Task Wander();
        Task SetLocation(ITileGrain tileGrain);
        Task<Wanderer> GetWanderer();
        Task SetInfo(Wanderer wanderer);
        Task SpeedUp(int ratio);
        Task SlowDown(int ratio);
    }
}

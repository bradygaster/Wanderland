using Orleans;

namespace Wanderland.Web.Shared;

public interface IWandererGrain : IWander, IGrainWithStringKey
{
    Task<Wanderer> GetWanderer();
    Task SetInfo(Wanderer wanderer);
}

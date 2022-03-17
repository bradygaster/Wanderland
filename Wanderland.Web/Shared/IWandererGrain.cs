using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IWandererGrain : IWander, IGrainWithStringKey
    {
    }
}

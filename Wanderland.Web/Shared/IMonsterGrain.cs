using Orleans;

namespace Wanderland.Web.Shared
{
    public interface IMonsterGrain : IWander, IGrainWithStringKey
    {
        Task Eat(IWandererGrain grain);
    }
}

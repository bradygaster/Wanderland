namespace Wanderland.Web.Shared
{
    public interface IMonsterGrain : IWanderGrain
    {
        Task Eat(IWanderGrain grain);
    }
}

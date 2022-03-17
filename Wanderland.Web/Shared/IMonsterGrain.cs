namespace Wanderland.Web.Shared
{
    public interface IMonsterGrain : IWandererGrain
    {
        Task Eat(IWandererGrain grain);
    }
}

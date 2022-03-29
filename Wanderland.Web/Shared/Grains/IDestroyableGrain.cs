using Orleans;

namespace Wanderland.Web.Shared;

public interface IDestroyableGrain : IGrain
{
    /// <summary>
    /// Called when the resource should stop processing and clean itself up.
    /// </summary>
    ValueTask OnDestroyWorld();
}

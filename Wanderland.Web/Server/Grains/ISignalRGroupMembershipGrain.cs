using Orleans;

namespace Wanderland.Web.Server.Grains
{
    public interface ISignalRGroupMembershipGrain : IGrainWithGuidKey
    {
        Task ChangeClientWorldGroupMembership(string connectionId, string worldName);
        Task ClientDisconnects(string connectionId);
    }
}

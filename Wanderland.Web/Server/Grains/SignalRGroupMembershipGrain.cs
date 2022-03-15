using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public interface ISignalRGroupMembershipGrain : IGrainWithGuidKey
    {
        Task ChangeClientWorldGroupMembership(string connectionId, string worldName);
        Task ClientDisconnects(string connectionId);
    }

    public class SignalRGroupMembershipGrain : Grain, ISignalRGroupMembershipGrain
    {
        private IPersistentState<List<SignalRConnectionToWorldMap>> GroupMemberships { get; }
        private IHubContext<WanderlandHub> WanderlandHub { get; }
        private ILogger<SignalRGroupMembershipGrain> Logger { get; }

        public SignalRGroupMembershipGrain(
            [PersistentState(Constants.PersistenceKeys.GroupStateName, Constants.PersistenceKeys.GroupStorageName)]
            IPersistentState<List<SignalRConnectionToWorldMap>> groupMemberships,
            IHubContext<WanderlandHub> wanderlandHub,
            ILogger<SignalRGroupMembershipGrain> logger)
        {
            GroupMemberships = groupMemberships;
            WanderlandHub = wanderlandHub;
            Logger = logger;
        }

        public async Task ChangeClientWorldGroupMembership(string connectionId, string worldName)
        {
            await ClientDisconnects(connectionId);
            await WanderlandHub.Groups.AddToGroupAsync(connectionId, worldName);
            GroupMemberships.State.Add(new SignalRConnectionToWorldMap { ConnectionId = connectionId, World = worldName });
            await GroupMemberships.WriteStateAsync();
        }

        public async Task ClientDisconnects(string connectionId)
        {
            foreach (var mapping in GroupMemberships.State.Where(x => x.ConnectionId == connectionId))
            {
                await WanderlandHub.Groups.RemoveFromGroupAsync(mapping.ConnectionId, mapping.World);
            }
            GroupMemberships.State.RemoveAll(x => x.ConnectionId == connectionId);
            await GroupMemberships.WriteStateAsync();
        }
    }

    public class SignalRConnectionToWorldMap
    {
        public string ConnectionId { get; set; }
        public string World { get; set; }
    }
}

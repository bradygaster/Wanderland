using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class SignalRGroupMembershipGrain : Grain, ISignalRGroupMembershipGrain
    {
        private IPersistentState<List<SignalRConnectionToWorldMap>> GroupMemberships { get; }
        private IHubContext<WanderlandHub> WanderlandHub { get; }

        public SignalRGroupMembershipGrain(
            [PersistentState(Constants.PersistenceKeys.GroupStateName, Constants.PersistenceKeys.GroupStorageName)]
            IPersistentState<List<SignalRConnectionToWorldMap>> groupMemberships,
            IHubContext<WanderlandHub> wanderlandHub)
        {
            GroupMemberships = groupMemberships;
            WanderlandHub = wanderlandHub;
        }

        public async Task ChangeClientWorldGroupMembership(string connectionId, string worldName)
        {
            if(GroupMemberships.State.Any(x => x.ConnectionId == connectionId))
            {
                await WanderlandHub.Groups.RemoveFromGroupAsync(connectionId, worldName);
                GroupMemberships.State.First(x => x.ConnectionId == connectionId).World = worldName;
            }
            else
            {
                GroupMemberships.State.Add(new SignalRConnectionToWorldMap {  ConnectionId = connectionId, World = worldName });
            }

            await GroupMemberships.WriteStateAsync();
        }

        public async Task ClientDisconnects(string connectionId)
        {
            if (GroupMemberships.State.Any(x => x.ConnectionId == connectionId))
            {
                GroupMemberships.State.RemoveAll(x => x.ConnectionId == connectionId);
            }

            await GroupMemberships.WriteStateAsync();
        }
    }

    public class SignalRConnectionToWorldMap
    {
        public string ConnectionId { get; set; }
        public string World { get; set; }
    }
}

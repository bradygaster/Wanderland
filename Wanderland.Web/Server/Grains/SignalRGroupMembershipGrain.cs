using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains;

public interface ISignalRGroupMembershipGrain : IGrainWithGuidKey
{
    Task ChangeClientWorldGroupMembership(string connectionId, string worldName);
    Task ClientDisconnects(string connectionId);
}

public class SignalRGroupMembershipGrain : Grain, ISignalRGroupMembershipGrain
{
    private readonly IPersistentState<List<SignalRConnectionToWorldMap>> _groupMemberships;
    private readonly IHubContext<WanderlandHub> _wanderlandHub;
    private readonly ILogger<SignalRGroupMembershipGrain> _logger;

    public SignalRGroupMembershipGrain(
        [PersistentState(
            stateName: Constants.PersistenceKeys.GroupStateName, 
            storageName: Constants.PersistenceKeys.GroupStorageName)]
        IPersistentState<List<SignalRConnectionToWorldMap>> groupMemberships,
        IHubContext<WanderlandHub> wanderlandHub,
        ILogger<SignalRGroupMembershipGrain> logger)
    {
        _groupMemberships = groupMemberships;
        _wanderlandHub = wanderlandHub;
        _logger = logger;
    }

    public async Task ChangeClientWorldGroupMembership(string connectionId, string worldName)
    {
        await ClientDisconnects(connectionId);
        await _wanderlandHub.Groups.AddToGroupAsync(connectionId, worldName);
        _groupMemberships.State.Add(new SignalRConnectionToWorldMap 
        { 
            ConnectionId = connectionId, 
            World = worldName 
        });
        await _groupMemberships.WriteStateAsync();
    }

    public async Task ClientDisconnects(string connectionId)
    {
        foreach (var mapping in _groupMemberships.State.Where(x => x.ConnectionId == connectionId))
        {
            await _wanderlandHub.Groups.RemoveFromGroupAsync(mapping.ConnectionId, mapping.World);
        }
        _groupMemberships.State.RemoveAll(x => x.ConnectionId == connectionId);
        await _groupMemberships.WriteStateAsync();
    }
}

[GenerateSerializer]
public class SignalRConnectionToWorldMap
{
    [Id(0)]
    public string ConnectionId { get; set; }

    [Id(1)]
    public string World { get; set; }
}

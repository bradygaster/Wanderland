using Microsoft.AspNetCore.SignalR;
using Orleans;
using Wanderland.Web.Server.Grains;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Hubs;

public class WanderlandHub : Hub<IWanderlandHubClient>
{
    private readonly ISignalRGroupMembershipGrain _groupMembershipGrain;

    public WanderlandHub(IGrainFactory grainFactory)
    {
        _groupMembershipGrain = grainFactory.GetGrain<ISignalRGroupMembershipGrain>(Guid.Empty);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _groupMembershipGrain.ClientDisconnects(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateWorldList()
    {
        await Clients.All.WorldListUpdated();
    }

    public async Task UpdateTile(Tile tile)
    {
        await Clients.Group(tile.World).TileUpdated(tile);
    }

    public async Task JoinWorld(string worldName)
    {
        await _groupMembershipGrain.ChangeClientWorldGroupMembership(Context.ConnectionId, worldName);
    }
}

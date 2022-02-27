using Microsoft.AspNetCore.SignalR;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Hubs
{
    public class WanderlandHub : Hub<IWanderlandHubClient>
    {
        public async Task UpdateWorldList()
        {
            await Clients.All.WorldListUpdated();
        }
        public async Task UpdateTile(Tile tile)
        {
            await Clients.All.TileUpdated(tile);
        }
    }
}

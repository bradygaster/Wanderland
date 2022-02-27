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
    }
}

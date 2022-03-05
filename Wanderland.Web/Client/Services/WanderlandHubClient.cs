using Microsoft.AspNetCore.SignalR.Client;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Client.Services
{
    public class WanderlandHubClient : IWanderlandHubClient
    {
        public WanderlandHubClient(Uri baseUri)
        {
            BaseUri = baseUri;
            HubUri = new Uri($"{BaseUri.AbsoluteUri}{Constants.Routes.WanderlandSignalRHubRoute}");
        }

        private HubConnection? Connection { get; set; }
        private Uri BaseUri { get; set; }
        private Uri HubUri { get; set; }
        public event EventHandler<WorldListUpdatedEventArgs> WorldListUpdate;
        public event EventHandler<TileUpdatedEventArgs> TileUpdate;

        public async Task Start()
        {
            Connection = new HubConnectionBuilder()
                .WithUrl(HubUri)
                .Build();

            var _ = async () =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await Connection.StartAsync();
            };

            Connection.Closed += async (error) => await _();

            Connection.On("WorldListUpdated", WorldListUpdated);
            Connection.On<Tile>("TileUpdated", TileUpdated);

            await Connection.StartAsync();
        }

        public async Task JoinWorld(string worldName)
        { 
            if(Connection != null)
            {
                await Connection.SendAsync("JoinWorld", worldName);
            }
        }

        public Task TileUpdated(Tile tile)
        {
            if (TileUpdate != null)
            {
                TileUpdate(this, new TileUpdatedEventArgs { Tile = tile });
            }
            return Task.CompletedTask;
        }

        public Task WorldListUpdated()
        {
            if (WorldListUpdate != null)
            {
                WorldListUpdate(this, new WorldListUpdatedEventArgs());
            }
            return Task.CompletedTask;
        }
    }
}

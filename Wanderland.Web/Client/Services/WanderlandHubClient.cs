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

        private HubConnection Connection { get; set; }
        private Uri BaseUri { get; set; }
        private Uri HubUri { get; set; }
        public event EventHandler<WorldListUpdatedEventArgs> WorldListUpdate;
        public event EventHandler<TileUpdatedEventArgs> TileUpdate;

        public async Task Start()
        {
            Connection = new HubConnectionBuilder()
                .WithUrl(HubUri)
                .Build();

            Connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await Connection.StartAsync();
            };

            Connection.On("WorldListUpdated", WorldListUpdated);
            Connection.On<Tile>("TileUpdated", TileUpdated);

            try
            {
                await Connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Task TileUpdated(Tile tile)
        {
            if (TileUpdate != null)
            {
                TileUpdate(this, new TileUpdatedEventArgs { Tile = tile });
            }

            Console.WriteLine("TileUpdated");
            return Task.CompletedTask;
        }

        public Task WorldListUpdated()
        {
            if (WorldListUpdate != null)
            {
                WorldListUpdate(this, new WorldListUpdatedEventArgs());
            }

            Console.WriteLine("WorldListUpdated");
            return Task.CompletedTask;
        }
    }
}

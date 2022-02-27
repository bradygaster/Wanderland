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
        public event EventHandler<WorldListUpdatedEventArgs> WorldListUpdated;

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

            Connection.On("WorldListUpdated", OnWorldListUpdated);

            try
            {
                await Connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public Task OnWorldListUpdated()
        {
            if (WorldListUpdated != null)
            {
                WorldListUpdated(this, new WorldListUpdatedEventArgs());
            }
            return Task.CompletedTask;
        }
    }
}

using Microsoft.AspNetCore.SignalR.Client;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Client.Services;

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

    public event Func<WorldListUpdatedEventArgs, Task> WorldListUpdate;
    public event Func<TileUpdatedEventArgs, Task> TileUpdate;
    public event Func<WorldAgeUpdatedEventArgs, Task> WorldAgeUpdate;
    public event Func<SystemStatusUpdateReceivedEventArgs, Task> SystemStatusUpdate;
    public event Func<PlayerListUpdatedEventArgs, Task> PlayerListUpdate;
    public event Func<PlayerUpdatedEventArgs, Task> PlayerUpdate;

    public async Task Start()
    {
        Connection = new HubConnectionBuilder()
            .WithUrl(HubUri)
            .WithAutomaticReconnect()
            .Build();

        Connection.On(nameof(WorldListUpdated), WorldListUpdated);
        Connection.On<Tile>(nameof(TileUpdated), TileUpdated);
        Connection.On<WorldAgeUpdatedEventArgs>(nameof(WorldAgeUpdated), WorldAgeUpdated);
        Connection.On<SystemStatusUpdateReceivedEventArgs>(nameof(SystemStatusReceived), SystemStatusReceived);
        Connection.On<PlayerListUpdatedEventArgs>(nameof(PlayerListUpdated), PlayerListUpdated);
        Connection.On<PlayerUpdatedEventArgs>(nameof(PlayerUpdated), PlayerUpdated);

        await Connection.StartAsync();
    }

    public string World { get; set; }

    public async Task JoinWorld(string worldName)
    {
        World = worldName;

        if (Connection is not null)
        {
            await Connection.SendAsync(nameof(JoinWorld), worldName);
        }
    }

    public Task TileUpdated(Tile tile) =>
        TileUpdate?.Invoke(
            new TileUpdatedEventArgs { Tile = tile }) ?? Task.CompletedTask;

    public Task WorldListUpdated() =>
        WorldListUpdate?.Invoke(
            new WorldListUpdatedEventArgs()) ?? Task.CompletedTask;

    public Task WorldAgeUpdated(WorldAgeUpdatedEventArgs args) =>
        WorldAgeUpdate?.Invoke(args) ?? Task.CompletedTask;

    public Task SystemStatusReceived(SystemStatusUpdateReceivedEventArgs args) =>
        SystemStatusUpdate?.Invoke(args) ?? Task.CompletedTask;

    public Task PlayerListUpdated(PlayerListUpdatedEventArgs args) =>
        PlayerListUpdate?.Invoke(args) ?? Task.CompletedTask;

    public Task PlayerUpdated(PlayerUpdatedEventArgs args) =>
        PlayerUpdate?.Invoke(args) ?? Task.CompletedTask;
}

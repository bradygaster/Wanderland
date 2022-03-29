using Microsoft.AspNetCore.SignalR.Client;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Client.Services;

public class WanderlandHubClient : IWanderlandHubClient
{
    readonly Uri _baseUri;
    readonly Uri _hubUri;

    HubConnection? _connection;

    public WanderlandHubClient(Uri baseUri)
    {
        _baseUri = baseUri;
        _hubUri = new Uri($"{_baseUri.AbsoluteUri}{Constants.Routes.WanderlandSignalRHubRoute}");
    }

    public event Func<WorldListUpdatedEventArgs, Task> WorldListUpdate;
    public event Func<TileUpdatedEventArgs, Task> TileUpdate;
    public event Func<WorldAgeUpdatedEventArgs, Task> WorldAgeUpdate;
    public event Func<SystemStatusUpdateReceivedEventArgs, Task> SystemStatusUpdate;
    public event Func<PlayerListUpdatedEventArgs, Task> PlayerListUpdate;
    public event Func<PlayerUpdatedEventArgs, Task> PlayerUpdate;

    public async Task Start()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUri)
            .WithAutomaticReconnect()
            .Build();

        _connection.On(nameof(WorldListUpdated), WorldListUpdated);
        _connection.On<Tile>(nameof(TileUpdated), TileUpdated);
        _connection.On<WorldAgeUpdatedEventArgs>(nameof(WorldAgeUpdated), WorldAgeUpdated);
        _connection.On<SystemStatusUpdateReceivedEventArgs>(nameof(SystemStatusReceived), SystemStatusReceived);
        _connection.On<PlayerListUpdatedEventArgs>(nameof(PlayerListUpdated), PlayerListUpdated);
        _connection.On<PlayerUpdatedEventArgs>(nameof(PlayerUpdated), PlayerUpdated);

        await _connection.StartAsync();
    }

    public string World { get; set; }

    public async Task JoinWorld(string worldName)
    {
        World = worldName;

        if (_connection is not null)
        {
            await _connection.SendAsync(nameof(JoinWorld), worldName);
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

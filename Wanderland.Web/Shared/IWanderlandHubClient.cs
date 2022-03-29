namespace Wanderland.Web.Shared;

public interface IWanderlandHubClient
{
    Task Start();
    Task WorldListUpdated();
    Task TileUpdated(Tile tile);
    Task WorldAgeUpdated(WorldAgeUpdatedEventArgs args);
    Task SystemStatusReceived(SystemStatusUpdateReceivedEventArgs args);
    Task PlayerListUpdated(PlayerListUpdatedEventArgs args);
    Task PlayerUpdated(PlayerUpdatedEventArgs args);
    Task JoinWorld(string worldName);
}

public class WorldListUpdatedEventArgs
{
}

public class WorldAgeUpdatedEventArgs
{
    public string World { get; set; }
    public TimeSpan Age { get; set; }
}

public class TileUpdatedEventArgs
{
    public Tile Tile { get; set; }
}

public class SystemStatusUpdateReceivedEventArgs
{
    public SystemStatusUpdate SystemStatusUpdate { get; set; }
}

public class PlayerListUpdatedEventArgs
{
    public List<Wanderer> Players { get; set; } = new List<Wanderer>();
}

public class PlayerUpdatedEventArgs
{
    public Wanderer Player { get; set; }
}

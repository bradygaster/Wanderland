using System.Diagnostics;

namespace Wanderland.Web.Shared
{
    public interface IWanderlandHubClient
    {
        Task Start();
        Task WorldListUpdated();
        Task TileUpdated(Tile tile);
        Task WorldAgeUpdated(WorldAgeUpdatedEventArgs args);
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
}

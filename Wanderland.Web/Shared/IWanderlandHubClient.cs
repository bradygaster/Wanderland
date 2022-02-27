namespace Wanderland.Web.Shared
{
    public interface IWanderlandHubClient
    {
        Task Start();
        Task WorldListUpdated();
        Task TileUpdated(Tile tile);
    }

    public class WorldListUpdatedEventArgs
    {
    }

    public class TileUpdatedEventArgs
    {
        public Tile Tile { get; set; }
    }
}

namespace Wanderland.Web.Shared
{
    public interface IWanderlandHubClient
    {
        Task Start();
        Task WorldListUpdated();
    }

    public class WorldListUpdatedEventArgs
    {
    }
}

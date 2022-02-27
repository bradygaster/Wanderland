namespace Wanderland.Web.Shared
{
    public interface IWanderlandHubClient
    {
        Task Start();
        Task OnWorldListUpdated();
        event EventHandler<WorldListUpdatedEventArgs> WorldListUpdated;
    }

    public class WorldListUpdatedEventArgs
    {
    }
}

namespace Wanderland.Web.Shared;

public static class Constants
{
    public static class PersistenceKeys
    {
        public const string WorldListStateName = "worldList";
        public const string WorldListStorageName = "worldListStorage";
        public const string WorldStateName = "world";
        public const string WorldStorageName = "worldStorage";
        public const string TileStateName = "tile";
        public const string TileStorageName = "tileStorage";
        public const string WandererStateName = "wanderer";
        public const string WandererStorageName = "wandererStorage";
        public const string GroupStateName = "group";
        public const string GroupStorageName = "groupStorage";
        public const string LobbyStateName = "lobby";
        public const string LobbyStorageName = "lobbyStorage";
    }

    public static class EnvironmentVariableNames
    {
        public const string ApplicationInsightsConnectionString = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    }

    public static class Routes
    {
        public const string WanderlandSignalRHubRoute = "hubs/wanderland";
    }
}

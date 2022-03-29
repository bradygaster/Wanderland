namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static bool IsDashboardEnabled(this IConfiguration configuration)
    {
        if (configuration.GetValue<string>("ENABLE_ORLEANS_DASHBOARD") is { Length: > 0 })
        {
            return configuration.GetValue<bool>("ENABLE_ORLEANS_DASHBOARD");
        }

        return false;
    }
}

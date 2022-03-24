namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        public static bool IsDashboardEnabled(this IConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(configuration.GetValue<string>("ENABLE_ORLEANS_DASHBOARD")))
            {
                if (configuration.GetValue<bool>("ENABLE_ORLEANS_DASHBOARD"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using Wanderland.Web.Shared;

namespace Wanderland.Web.Server
{
    public static class AzureMonitoringModule
    {
        public static WebApplicationBuilder SetupApplicationInsights(this WebApplicationBuilder builder)
        {
            if (builder.Configuration.GetValue<string>(
                    Constants.EnvironmentVariableNames.ApplicationInsightsConnectionString)
                    is { Length: > 0 } connectionString)
            {
                builder.Services.AddApplicationInsightsTelemetry(connectionString);
            }

            return builder;
        }
    }
}

using Wanderland.Web.Shared;

namespace Wanderland.Web.Server
{
    public static class AzureMonitoringModule
    {
        public static WebApplicationBuilder SetupApplicationInsights(this WebApplicationBuilder builder)
        {
            builder.Services.AddApplicationInsightsTelemetry(
                builder.Configuration.GetValue<string>(Constants.EnvironmentVariableNames.ApplicationInsightsConnectionString)
            );

            return builder;
        }
    }
}

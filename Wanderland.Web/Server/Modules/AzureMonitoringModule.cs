using Wanderland.Web.Shared;

namespace Wanderland.Web.Server
{
    public static class AzureMonitoringModule
    {
        public static WebApplicationBuilder SetupApplicationInsights(this WebApplicationBuilder builder)
        {
            var aiCnStr = builder.Configuration.GetValue<string>(Constants.EnvironmentVariableNames.ApplicationInsightsConnectionString);
            if(!string.IsNullOrEmpty(aiCnStr))
            {
                builder.Services.AddApplicationInsightsTelemetry(aiCnStr);
            }

            return builder;
        }
    }
}

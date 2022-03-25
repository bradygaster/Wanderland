using Orleans;
using Orleans.Hosting;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server
{
    internal static class OrleansSiloModule
    {
        internal static WebApplicationBuilder SetupOrleansSilo(this WebApplicationBuilder builder)
        {
            builder.Host.UseOrleans((siloBuilder) =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.UseInMemoryReminderService();
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldListStorageName);
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldStorageName);
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.TileStorageName);
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WandererStorageName);
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.GroupStorageName);
                siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.LobbyStorageName);
                siloBuilder.AddActivityPropagation();

                if(!string.IsNullOrEmpty(builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY")))
                {
                    siloBuilder.AddApplicationInsightsTelemetryConsumer(
                        builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY")
                    );
                }
            });

            return builder;
        }
    }
}

﻿using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

using Orleans;
using Orleans.Hosting;
using OrleansDashboard;

namespace Wanderland.Web.Server
{
    internal static class SetupModule
    {
        internal static WebApplicationBuilder SetupServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            return builder;
        }

        internal static WebApplication SetupApp(this WebApplication app)
        {
            app.UseStaticFiles();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapRazorPages();
            app.UseOrleansDashboard();
            app.Map("/dashboard", d =>
            {
                d.UseOrleansDashboard();
            });
            app.MapHub<WanderlandHub>($"/{Constants.Routes.WanderlandSignalRHubRoute}");
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}

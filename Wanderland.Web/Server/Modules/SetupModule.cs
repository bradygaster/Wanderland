namespace Wanderland.Web.Server
{
    internal static class SetupModule
    {
        internal static WebApplicationBuilder SetupServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddRazorPages();

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
            app.MapFallbackToFile("index.html");

            return app;
        }
    }
}

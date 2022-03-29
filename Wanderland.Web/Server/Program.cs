using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans;
using Wanderland.Web.Server;
using Wanderland.Web.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.SetupServices();
builder.SetupOrleansSilo();
builder.SetupApplicationInsights();

builder.Services.AddOpenTelemetryTracing(b =>
{
    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Wanderland.Server"));
    b.AddSource("orleans.runtime.graincall");
    b.AddJaegerExporter(o =>
    {
        o.AgentHost = "localhost";
        o.AgentPort = 6831;
    });
    b.AddAspNetCoreInstrumentation();
    b.AddConsoleExporter();
    b.AddHttpClientInstrumentation();
});

var app = builder.Build();

app.SetupApp();
app.MapWanderlandApiEndpoints();

app.Run();
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Wanderland.Web.Client;
using Wanderland.Web.Client.Services;
using Wanderland.Web.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<WanderlandHubClient>(
    _ => new WanderlandHubClient(
        new Uri(builder.HostEnvironment.BaseAddress)));
builder.Services.AddScoped<IWanderlandHttpApiClient>(
    _ => new WanderlandHttpApiClient(
        new HttpClient
        {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
        }));
builder.Services.AddMudServices();

await builder.Build().RunAsync();
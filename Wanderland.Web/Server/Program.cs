using Bogus;
using Orleans;
using Orleans.Hosting;
using Wanderland.Web.Server;
using Wanderland.Web.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.SetupServices();
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldListStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.TileStorageName);
    siloBuilder.UseDashboard();
});

var app = builder.Build();
app.SetupApp();

app.MapPost("/worlds/new", async (IGrainFactory grainFactory, int rows, int columns) =>
{
    var faker = new Faker();
    var name = $"{faker.Address.City()}".ToLower().Replace(" ", "-");
    var worldGrain = await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = name, Rows = rows, Columns = columns });
    return await worldGrain.GetWorld();
})
.WithName("CreateNewWorld")
.Produces<World>(StatusCodes.Status200OK);

app.MapGet("/worlds", async (IGrainFactory grainFactory) =>
    await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()
)
.WithName("GetWorlds")
.Produces<List<World>>(StatusCodes.Status200OK);

app.Run();
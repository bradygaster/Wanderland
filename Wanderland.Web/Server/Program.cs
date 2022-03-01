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
    siloBuilder.UseInMemoryReminderService();
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldListStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WorldStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.TileStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.WandererStorageName);
    siloBuilder.AddMemoryGrainStorage(Constants.PersistenceKeys.GroupStorageName);
    siloBuilder.UseDashboard();
});

var app = builder.Build();
app.SetupApp();

// create a new world
app.MapPost("/worlds", async (IGrainFactory grainFactory, int rows, int columns) =>
{
    if (rows > 10 || columns > 10) return Results.BadRequest("World max size is 10x10.");
    var creator = grainFactory.GetGrain<ICreatorGrain>(Guid.Empty);

    var faker = new Faker();
    var name = $"{faker.Address.City()}".ToLower().Replace(" ", "-");

    var exists = await creator.WorldExists(name);
    if (exists) return Results.Conflict($"World with name {name} already exists.");

    var worldGrain = await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = name, Rows = rows, Columns = columns });
    if (worldGrain != null)
    {
        var newWorld = await worldGrain.GetWorld();
        return Results.Created($"/worlds/{newWorld.Name}", newWorld);
    }

    return Results.BadRequest();
})
.WithName("CreateNewWorld")
.Produces(StatusCodes.Status409Conflict)
.Produces(StatusCodes.Status400BadRequest)
.Produces<World>(StatusCodes.Status201Created);

// gets all the worlds in the list
app.MapGet("/worlds", async (IGrainFactory grainFactory) =>
    await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()
)
.WithName("GetWorlds")
.Produces<List<World>>(StatusCodes.Status200OK);

// gets a specific world by name
app.MapGet("/worlds/{name}", async (IGrainFactory grainFactory, string name) =>
{
    var world = (await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()).FirstOrDefault(w => w.Name.ToLower() == name.ToLower());
    if (world == null) return Results.NotFound();
    else return Results.Ok(world);
})
.WithName("GetWorld")
.Produces(StatusCodes.Status404NotFound)
.Produces<World>(StatusCodes.Status200OK);

// creates a new wanderer in the world
app.MapPost("/worlds/{worldName}/wanderers/{wandererName}", async (IGrainFactory grainFactory, string worldName, string wandererName) =>
{
    var world = (await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()).FirstOrDefault(w => w.Name.ToLower() == worldName.ToLower());
    if (world == null) return Results.NotFound();

    var newWandererGrain = grainFactory.GetGrain<IWanderGrain>(wandererName);
    var nextTileGrainId = $"{worldName}/{new Random().Next(0, world.Rows - 1)}/{new Random().Next(0, world.Columns - 1)}";
    await newWandererGrain.SetLocation(grainFactory.GetGrain<ITileGrain>(nextTileGrainId));

    return Results.Ok(await newWandererGrain.GetWanderer());
})
.WithName("CreateWanderer")
.Produces(StatusCodes.Status404NotFound)
.Produces<Wanderer>(StatusCodes.Status200OK);

// gets all the tiles for a specific world
app.MapGet("/worlds/{name}/tiles", async (IGrainFactory grainFactory, string name) =>
{
    var world = (await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()).FirstOrDefault(w => w.Name.ToLower() == name.ToLower());
    if (world == null) return Results.NotFound();
    var tiles = new List<Tile>();

    for (int row = 0; row < world.Rows; row++)
    {
        for (int col = 0; col < world.Columns; col++)
        {
            string grainKey = $"{world.Name}/{row}/{col}";
            var tileGrain = grainFactory.GetGrain<ITileGrain>(grainKey);
            tiles.Add(await tileGrain.GetTile());
        }
    }

    return Results.Ok(tiles);
})
.WithName("GetWorldTiles")
.Produces(StatusCodes.Status404NotFound)
.Produces<World>(StatusCodes.Status200OK);

// gets a tile's detail
app.MapGet("/worlds/{name}/tiles/{row}/{column}", async (IGrainFactory grainFactory, string name, int row, int column) =>
{
    var creator = grainFactory.GetGrain<ICreatorGrain>(Guid.Empty);
    name = name.ToLower();

    if (!await creator.WorldExists(name))
        return Results.NotFound($"World {name} was not found.");

    var tileGrain = grainFactory.GetGrain<ITileGrain>($"{name}/{row}/{column}");
    var tile = await tileGrain.GetTile();
    return Results.Ok(tile);
})
.WithName("GetTileCurrentState")
.Produces(StatusCodes.Status404NotFound)
.Produces<Tile>(StatusCodes.Status200OK);

// create an entire world in one request
app.MapPost("/worlds/{worldName}/{rows}/{cols}/{wanderers}", async (IGrainFactory grainFactory,
    string worldName,
    int rows,
    int columns,
    int wanderers) =>
{
    if (rows > 10 || columns > 10) return Results.BadRequest("World max size is 10x10.");
    var creator = grainFactory.GetGrain<ICreatorGrain>(Guid.Empty);

    var exists = await creator.WorldExists(worldName);
    if (exists) return Results.Conflict($"World with name {worldName} already exists.");

    var worldGrain = await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = worldName, Rows = rows, Columns = columns });
    if (worldGrain != null)
    {
        var newWorld = await worldGrain.GetWorld();

        var faker = new Faker();
        for (int i = 0; i < wanderers; i++)
        {
            string wandererName = new Faker().Person.FirstName;
            var newWandererGrain = grainFactory.GetGrain<IWanderGrain>(wandererName);
            var nextTileGrainId = $"{worldName}/{new Random().Next(0, newWorld.Rows - 1)}/{new Random().Next(0, newWorld.Columns - 1)}";
            await newWandererGrain.SetLocation(grainFactory.GetGrain<ITileGrain>(nextTileGrainId));
        }

        return Results.Ok(newWorld);
    }

    return Results.BadRequest();
})
.WithName("CreateWholeWorldDemo")
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.Produces<World>(StatusCodes.Status200OK);

// create an entire world in one request but do it completely randomly
app.MapPost("/worlds/random", async (IGrainFactory grainFactory) =>
{
    var faker = new Faker();
    int rows = new Random().Next(5, 10);
    int columns = new Random().Next(5, 10);
    var creator = grainFactory.GetGrain<ICreatorGrain>(Guid.Empty);
    var worldName = $"{new Faker().Address.City().ToLower().Replace(" ", "-")}";
    var worldGrain = await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = worldName, Rows = rows, Columns = columns });
    if (worldGrain != null)
    {
        var newWorld = await worldGrain.GetWorld();
        int wanderers = new Random().Next(1, 10);
        for (int i = 0; i < wanderers; i++)
        {
            string wandererName = new Faker().Person.FirstName;
            var newWandererGrain = grainFactory.GetGrain<IWanderGrain>(wandererName);
            var nextTileGrainId = $"{worldName}/{new Random().Next(0, newWorld.Rows - 1)}/{new Random().Next(0, newWorld.Columns - 1)}";
            await newWandererGrain.SetLocation(grainFactory.GetGrain<ITileGrain>(nextTileGrainId));
        }

        return Results.Ok(newWorld);
    }

    return Results.BadRequest();
})
.WithName("CreateRandomLand")
.Produces(StatusCodes.Status400BadRequest)
.Produces<World>(StatusCodes.Status200OK);

app.Run();
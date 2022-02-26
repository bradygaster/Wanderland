﻿using Bogus;
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

// create a new world
app.MapPost("/worlds/new", async (IGrainFactory grainFactory, int rows, int columns) =>
{
    if (rows > 10 || columns > 10) return Results.BadRequest("World max size is 10x10.");
    var creator = grainFactory.GetGrain<ICreatorGrain>(Guid.Empty);

    var faker = new Faker();
    var name = $"{faker.Address.City()}".ToLower().Replace(" ", "-");

    var exists = await creator.WorldExists(name);
    if (exists) return Results.Conflict($"World with name {name} already exists.");

    var worldGrain = await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = name, Rows = rows, Columns = columns });
    var newWorld = await worldGrain.GetWorld();
    return Results.Created($"/worlds/{newWorld.Name}", newWorld);
})
.WithName("CreateNewWorld")
.Produces(StatusCodes.Status409Conflict)
.Produces(StatusCodes.Status400BadRequest)
.Produces<World>(StatusCodes.Status201Created);

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

// gets all the worlds in the list
app.MapGet("/worlds", async (IGrainFactory grainFactory) =>
    await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()
)
.WithName("GetWorlds")
.Produces<List<World>>(StatusCodes.Status200OK);

app.Run();
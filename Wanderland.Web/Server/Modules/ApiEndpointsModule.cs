using Orleans;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server
{
    public static class ApiEndpointsModule
    {
        public static WebApplication MapWanderlandApiEndpoints(this WebApplication app)
        {
            // puts a player in the lobby
            app.MapGet("/lobby", async (IGrainFactory grainFactory, string playerName) =>
            {
                var lobbyGrain = grainFactory.GetGrain<ILobbyGrain>(Guid.Empty);
                await lobbyGrain.JoinLobby(new Wanderer
                {
                    Name = playerName
                });
            })
            .WithName("JoinLobby")
            .Produces(StatusCodes.Status200OK);

            // gets all the worlds in the list
            app.MapGet("/worlds", async (IGrainFactory grainFactory) =>
                await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()
            )
            .WithName("GetWorlds")
            .Produces<List<World>>(StatusCodes.Status200OK);

            // delete the world with the given name
            app.MapDelete("/worlds/{name}", async (IGrainFactory grainFactory, string name) =>
            {
                await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).DestroyWorld(
                    grainFactory.GetGrain<IWorldGrain>(name));
            })
            .WithName("DeleteWorld")
            .Produces(StatusCodes.Status200OK);

            // gets a specific world by name
            app.MapGet("/worlds/{name}", async (IGrainFactory grainFactory, string name) =>
            {
                var world = (await grainFactory.GetGrain<ICreatorGrain>(Guid.Empty).GetWorlds()).FirstOrDefault(w => w.Name.ToLower() == name.ToLower());
                return world is null ? Results.NotFound() : Results.Ok(world);
            })
            .WithName("GetWorld")
            .Produces(StatusCodes.Status404NotFound)
            .Produces<World>(StatusCodes.Status200OK);

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

                if (tile is { ThingsHere.Count: > 0 } and { Type: TileType.Barrier })
                {
                    tile.ThingsHere.Clear();
                }

                return Results.Ok(tile);
            })
            .WithName("GetTileCurrentState")
            .Produces(StatusCodes.Status404NotFound)
            .Produces<Tile>(StatusCodes.Status200OK);

            return app;
        }
    }
}

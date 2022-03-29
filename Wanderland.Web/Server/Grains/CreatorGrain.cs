using Bogus;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains;

public class CreatorGrain : Grain, ICreatorGrain
{
    IDisposable _timer;

    readonly IPersistentState<List<string>> _worlds;
    readonly IHubContext<WanderlandHub, IWanderlandHubClient> _wanderlandHubContext;
    readonly ILogger<CreatorGrain> _logger;
    readonly DateTime _dateStarted;

    public int WorldsCompleted { get; set; }

    public CreatorGrain(
        [PersistentState(
            stateName: Constants.PersistenceKeys.WorldListStateName,
            storageName: Constants.PersistenceKeys.WorldListStorageName)]
        IPersistentState<List<string>> worlds,
        IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub,
        ILogger<CreatorGrain> logger)
    {
        _worlds = worlds;
        _wanderlandHubContext = wanderlandHub;
        _logger = logger;
        _dateStarted = DateTime.Now;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _timer = RegisterTimer(async _ =>
        {
            var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
            var grainCount = await managementGrain.GetTotalActivationCount();

            await _wanderlandHubContext.Clients.All.SystemStatusReceived(new SystemStatusUpdateReceivedEventArgs
            {
                SystemStatusUpdate = new SystemStatusUpdate
                {
                    DateStarted = _dateStarted,
                    GrainsActive = grainCount,
                    TimeUp = DateTime.Now - _dateStarted,
                    WorldsCompleted = this.WorldsCompleted
                }
            });

            var removes = new List<string>();
            foreach (var world in _worlds.State)
            {
                var worldGrain = GrainFactory.GetGrain<IWorldGrain>(world);
                if (await worldGrain.IsWorldEmpty())
                {
                    removes.Add(world);
                }
            }

            if (removes.Any())
            {
                foreach (var remove in removes)
                {
                    WorldsCompleted += 1;
                    await _wanderlandHubContext.Clients.All.WorldListUpdated();
                    _worlds.State.Remove(remove);
                    var worldGrain = GrainFactory.GetGrain<IWorldGrain>(remove);
                }
            }

            if (!_worlds.State.Any())
            {
                await GenerateNewWorld();
            }

        }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

        return base.OnActivateAsync(cancellationToken);
    }

    async Task<IWorldGrain?> ICreatorGrain.CreateWorld(World world)
    {
        if (world is { Name.Length: > 0 })
        {
            if (!_worlds.State.Any(x => x.ToLower() == world.Name.ToLower()))
            {
                _worlds.State.Add(world.Name);
            }

            var worldGrain = GrainFactory.GetGrain<IWorldGrain>(world.Name.ToLower());
            await worldGrain.SetWorld(world);

            for (int row = 0; row < world.Rows; row++)
            {
                for (int col = 0; col < world.Columns; col++)
                {
                    await worldGrain.SetTile(new Tile
                    {
                        Row = row,
                        Column = col,
                        Type = CalculateTileTypeBasedOnWorldSize(world.Rows, world.Columns),
                        World = world.Name
                    });
                }
            }

            await _wanderlandHubContext.Clients.All.WorldListUpdated();
            return worldGrain;
        }

        return null;
    }

    TileType CalculateTileTypeBasedOnWorldSize(int rows, int cols)
    {
        var rndint = Random.Shared.Next(1, 12);
        return (rndint % 4 == 0) ? TileType.Barrier : TileType.Space;
    }

    Task<bool> ICreatorGrain.WorldExists(string name) =>
        Task.FromResult(_worlds.State.Any(
            x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)));

    async Task<List<World>> ICreatorGrain.GetWorlds()
    {
        var result = new List<World>();
        foreach (var worldName in _worlds.State)
        {
            result.Add(await GrainFactory.GetGrain<IWorldGrain>(worldName.ToLower()).GetWorld());
        }
        return result;
    }

    public async Task DestroyWorld(IWorldGrain worldGrain)
    {

        var world = await worldGrain.GetWorld();
        foreach (var tile in world.Tiles.Where(x => x.ThingsHere.Count > 0))
        {
            foreach (var thing in tile.ThingsHere)
            {
                if (thing is Wanderer && thing is not Monster)
                {
                    var grain = GrainFactory.GetGrain<IWandererGrain>(thing.Name);
                    if (grain != null)
                    {
                        var lobbyGrain = GrainFactory.GetGrain<ILobbyGrain>(Guid.Empty);
                        await lobbyGrain.JoinLobby(await grain.GetWanderer());
                    }
                }
            }
        }

        _worlds.State.RemoveAll(x => x == worldGrain.GetPrimaryKeyString());
        await _wanderlandHubContext.Clients.All.WorldListUpdated();
        WorldsCompleted += 1;
    }

    public async Task GenerateNewWorld()
    {
        var faker = new Faker();
        int rows = 8;
        int columns = 8;
        var lobbyGrain = GrainFactory.GetGrain<ILobbyGrain>(Guid.Empty);

        var worldName = $"{faker.Address.City().ToLower().Replace(" ", "-")}";
        var worldGrain = await GrainFactory.GetGrain<ICreatorGrain>(Guid.Empty)
            .CreateWorld(new World { Name = worldName, Rows = rows, Columns = columns });
        if (worldGrain is not null)
        {
            var newWorld = await worldGrain.GetWorld();

            var wanderersFromLobby = await lobbyGrain.GetPlayersForNextWorld();
            foreach (var wanderer in wanderersFromLobby)
            {
                string wandererName = wanderer.Name;
                var newWandererGrain = GrainFactory.GetGrain<IWandererGrain>(wandererName, grainClassNamePrefix: typeof(WandererGrain).FullName);
                await newWandererGrain.SetInfo(new Wanderer
                {
                    Name = wandererName,
                    Speed = Random.Shared.Next(400, 800)
                });
                var nextTileGrainId = $"{worldName}/{Random.Shared.Next(0, newWorld.Rows - 1)}/{Random.Shared.Next(0, newWorld.Columns - 1)}";
                var tileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
                var tile = await tileGrain.GetTile();

                tile.ThingsHere.Clear();
                await tileGrain.SetTile(tile);

                if (tile.Type is not TileType.Barrier)
                {
                    await newWandererGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(nextTileGrainId));
                }
                else
                {
                    while (tile.Type is TileType.Barrier)
                    {
                        nextTileGrainId = $"{worldName}/{Random.Shared.Next(0, newWorld.Rows - 1)}/{Random.Shared.Next(0, newWorld.Columns - 1)}";
                        tile = await GrainFactory.GetGrain<ITileGrain>(nextTileGrainId).GetTile();
                    }
                    await lobbyGrain.LeaveLobby(wanderer);
                    await newWandererGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(nextTileGrainId));
                }
            }

            // now add a monster
            var monsterName = $"monster-{Environment.TickCount}";
            var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(monsterName, grainClassNamePrefix: typeof(MonsterGrain).FullName);
            await monsterGrain.SetInfo(new Monster
            {
                Name = monsterName,
                Speed = Random.Shared.Next(500, 800)
            });
            var monsterTileGrainId = $"{worldName}/{Random.Shared.Next(0, newWorld.Rows - 1)}/{Random.Shared.Next(0, newWorld.Columns - 1)}";
            await monsterGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(monsterTileGrainId));
        }
    }
}

﻿using Bogus;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class CreatorGrain : Grain, ICreatorGrain
    {
        public IPersistentState<List<string>> Worlds { get; }
        public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHubContext { get; }
        public ILogger<CreatorGrain> Logger { get; }
        public DateTime DateStarted { get; }
        public int WorldsCompleted { get; set; }

        public CreatorGrain([PersistentState(Constants.PersistenceKeys.WorldListStateName, Constants.PersistenceKeys.WorldListStorageName)]
            IPersistentState<List<string>> worlds,
            IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub, 
            ILogger<CreatorGrain> logger
            )
        {
            Worlds = worlds;
            WanderlandHubContext = wanderlandHub;
            Logger = logger;
            DateStarted = DateTime.Now;
        }

        IDisposable _timer;
        public override Task OnActivateAsync()
        {
            _timer?.Dispose();
            _timer = RegisterTimer(async _ =>
            {
                var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
                var grainCount = await managementGrain.GetTotalActivationCount();

                await WanderlandHubContext.Clients.All.SystemStatusReceived(new SystemStatusUpdateReceivedEventArgs
                {
                    SystemStatusUpdate = new SystemStatusUpdate
                    {
                        DateStarted = DateStarted,
                        GrainsActive = grainCount,
                        TimeUp = DateTime.Now - DateStarted,
                        WorldsCompleted = this.WorldsCompleted
                    }
                });

                var removes = new List<string>();
                foreach (var world in Worlds.State)
                {
                    var worldGrain = GrainFactory.GetGrain<IWorldGrain>(world);
                    if(await worldGrain.IsWorldEmpty())
                    {
                        removes.Add(world);
                    }
                }

                if (removes.Any())
                {
                    foreach (var remove in removes)
                    {
                        WorldsCompleted += 1;
                        await WanderlandHubContext.Clients.All.WorldListUpdated();
                        Worlds.State.Remove(remove);
                    }
                }

                if(!Worlds.State.Any())
                {
                    await GenerateNewWorld();
                }

            }, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

            return base.OnActivateAsync();
        }

        async Task<IWorldGrain?> ICreatorGrain.CreateWorld(World world)
        {
            if(!string.IsNullOrEmpty(world.Name))
            {
                if (!Worlds.State.Any(x => x.ToLower() == world.Name.ToLower()))
                {
                    Worlds.State.Add(world.Name);
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

                await WanderlandHubContext.Clients.All.WorldListUpdated();
                return worldGrain;
            }

            return null;
        }

        TileType CalculateTileTypeBasedOnWorldSize(int rows, int cols)
        {
            var rndint = new Random().Next(1, 12);
            return (rndint % 4 == 0) ? TileType.Barrier : TileType.Space;
        }

        Task<bool> ICreatorGrain.WorldExists(string name)
        {
            return Task.FromResult(Worlds.State.Any(x => x == name.ToLower()));
        }

        async Task<List<World>> ICreatorGrain.GetWorlds()
        {
            var result = new List<World>();
            foreach (var worldName in Worlds.State)
            {
                result.Add(await GrainFactory.GetGrain<IWorldGrain>(worldName.ToLower()).GetWorld());
            }
            return result;
        }

        public async Task DestroyWorld(IWorldGrain worldGrain)
        {
            Worlds.State.RemoveAll(x => x == worldGrain.GetPrimaryKeyString());
            await WanderlandHubContext.Clients.All.WorldListUpdated();
            WorldsCompleted += 1;
        }

        public async Task GenerateNewWorld()
        {
            var faker = new Faker();
            int rows = 8;
            int columns = 8;
            var creator = GrainFactory.GetGrain<ICreatorGrain>(Guid.Empty);
            var worldName = $"{new Faker().Address.City().ToLower().Replace(" ", "-")}";
            var worldGrain = await GrainFactory.GetGrain<ICreatorGrain>(Guid.Empty).CreateWorld(new World { Name = worldName, Rows = rows, Columns = columns });
            if (worldGrain != null)
            {
                var newWorld = await worldGrain.GetWorld();
                int wanderers = 10;

                // add some wanderers
                for (int i = 0; i < wanderers; i++)
                {
                    string wandererName = new Faker().Person.FirstName;
                    var newWandererGrain = GrainFactory.GetGrain<IWandererGrain>(wandererName, grainClassNamePrefix: typeof(WandererGrain).FullName);
                    await newWandererGrain.SetInfo(new Wanderer
                    {
                        Name = wandererName,
                        Speed = new Random().Next(300, 800)
                    });
                    var nextTileGrainId = $"{worldName}/{new Random().Next(0, newWorld.Rows - 1)}/{new Random().Next(0, newWorld.Columns - 1)}";
                    var tileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
                    var tile = await tileGrain.GetTile();

                    tile.ThingsHere.Clear();
                    await tileGrain.SetTile(tile);

                    if (tile.Type != TileType.Barrier)
                    {
                        await newWandererGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(nextTileGrainId));
                    }
                    else
                    {
                        while (tile.Type == TileType.Barrier)
                        {
                            nextTileGrainId = $"{worldName}/{new Random().Next(0, newWorld.Rows - 1)}/{new Random().Next(0, newWorld.Columns - 1)}";
                            tile = await GrainFactory.GetGrain<ITileGrain>(nextTileGrainId).GetTile();
                        }
                        await newWandererGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(nextTileGrainId));
                    }
                }

                // now add a monster
                var monsterName = $"monster-{Environment.TickCount}";
                var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(monsterName, grainClassNamePrefix: typeof(MonsterGrain).FullName);
                await monsterGrain.SetInfo(new Monster
                {
                    Name = monsterName,
                    Speed = new Random().Next(200, 500)
                });
                var monsterTileGrainId = $"{worldName}/{new Random().Next(0, newWorld.Rows - 1)}/{new Random().Next(0, newWorld.Columns - 1)}";
                await monsterGrain.SetLocation(GrainFactory.GetGrain<ITileGrain>(monsterTileGrainId));
            }
        }
    }
}

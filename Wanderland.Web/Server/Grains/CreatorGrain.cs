using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class CreatorGrain : Grain, ICreatorGrain
    {
        private readonly Random _random = new ();
        public IPersistentState<List<string>> Worlds { get; }
        public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHubContext { get; }

        public CreatorGrain([PersistentState(Constants.PersistenceKeys.WorldListStateName, Constants.PersistenceKeys.WorldListStorageName)]
            IPersistentState<List<string>> worlds,
            IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHub
            )
        {
            Worlds = worlds;
            WanderlandHubContext = wanderlandHub;
        }

        async Task<IWorldGrain?> ICreatorGrain.CreateWorld(World world)
        {
            if (string.IsNullOrEmpty(world.Name))
            {
                return null;
            }

            if (!Worlds.State.Any(x => string.Equals(x, world.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Worlds.State.Add(world.Name);
            }

            var worldGrain = GrainFactory.GetGrain<IWorldGrain>(world.Name.ToLower());
            await worldGrain.SetWorld(world);

            for (int row = 0; row < world.Rows; row++)
            {
                for (int col = 0; col < world.Columns; col++)
                {
                    await worldGrain.MakeTile(new Tile
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

        TileType CalculateTileTypeBasedOnWorldSize(int rows, int cols)
        {
            var rndint = _random.Next(1, 12);
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
    }
}

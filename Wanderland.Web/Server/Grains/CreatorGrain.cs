using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class CreatorGrain : Grain, ICreatorGrain
    {
        public IPersistentState<List<string>> Worlds { get; }

        public CreatorGrain([PersistentState(Constants.PersistenceKeys.WorldListStateName, Constants.PersistenceKeys.WorldListStorageName)]
            IPersistentState<List<string>> worlds
            )
        {
            Worlds = worlds;
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
                        await worldGrain.MakeTile(new Tile
                        {
                            Row = row,
                            Column = col,
                            Type = TileType.Space,
                            World = world.Name
                        });
                    }
                }

                return worldGrain;
            }

            return null;
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

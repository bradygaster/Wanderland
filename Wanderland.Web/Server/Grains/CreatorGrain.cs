using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class CreatorGrain : Grain, ICreatorGrain
    {
        IGrainFactory _grainFactory;
        IPersistentState<List<string>> _worlds;

        public CreatorGrain(IGrainFactory grainFactory,
            [PersistentState(Constants.PersistenceKeys.WorldListStateName, Constants.PersistenceKeys.WorldListStorageName)]
            IPersistentState<List<string>> worlds
            )
        {
            _grainFactory = grainFactory;
            _worlds = worlds;
        }

        async Task<IWorldGrain> ICreatorGrain.CreateWorld(World world)
        {
            if (!_worlds.State.Any(x => x.ToLower() == world.Name.ToLower()))
            {
                _worlds.State.Add(world.Name);
            }

            await _worlds.WriteStateAsync();

            var worldGrain = _grainFactory.GetGrain<IWorldGrain>(world.Name.ToLower());
            await worldGrain.SetWorld(world);

            for (int row = 0; row < world.Rows; row++)
            {
                for (int col = 0; col < world.Columns; col++)
                {
                    await worldGrain.MakeTile(new Tile
                    {
                        Row = row,
                        Column = col,
                        Type = TileType.Space
                    });
                }
            }

            return worldGrain;
        }

        Task<bool> ICreatorGrain.WorldExists(string name)
        {
            return Task.FromResult(_worlds.State.Any(x => x == name.ToLower()));
        }

        async Task<List<World>> ICreatorGrain.GetWorlds()
        {
            var result = new List<World>();
            foreach (var worldName in _worlds.State)
            {
                result.Add(await _grainFactory.GetGrain<IWorldGrain>(worldName.ToLower()).GetWorld());
            }
            return result;
        }
    }
}

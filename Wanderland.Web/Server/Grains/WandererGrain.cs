using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class WandererGrain : Grain, IWanderGrain
    {
        public WandererGrain([PersistentState(Constants.PersistenceKeys.WandererStateName, Constants.PersistenceKeys.WandererStorageName)]
            IPersistentState<Wanderer> wanderer, ILogger<WandererGrain> logger)
        {
            Wanderer = wanderer;
            Logger = logger;
        }

        public IPersistentState<Wanderer> Wanderer { get; }
        public ILogger<WandererGrain> Logger { get; }

        public Task<Wanderer> GetWanderer()
        {
            Wanderer.State.Name = this.GetPrimaryKeyString();
            return Task.FromResult(Wanderer.State);
        }

        public override async Task OnActivateAsync()
        {
            RegisterTimer(async _ =>
                {
                    await Wander();
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            await base.OnActivateAsync();
        }

        public async Task SetLocation(ITileGrain tileGrain)
        {
            Wanderer.State.Name = this.GetPrimaryKeyString();
            Wanderer.State.CurrentLocation = await tileGrain.GetTile();
            await tileGrain.Arrives(this);
        }

        public async Task Wander()
        {
            var world = await GrainFactory.GetGrain<IWorldGrain>(Wanderer.State.CurrentLocation.World).GetWorld();

            // Probe the adjacent tiles to see if they are available.
            var current = Wanderer.State.CurrentLocation;
            var tasks = new List<Task<(bool, int, int)>>
            {
                IsMoveAvailable(GrainFactory, world, current.Row - 1, current.Column),
                IsMoveAvailable(GrainFactory, world, current.Row + 1, current.Column),
                IsMoveAvailable(GrainFactory, world, current.Row, current.Column - 1),
                IsMoveAvailable(GrainFactory, world, current.Row, current.Column + 1),
            };

            // Wait for all probes to finish.
            await Task.WhenAll(tasks);

            // Save up the list of available options for our next direction.
            var options = new List<string>();
            foreach (var task in tasks)
            {
                var (isAvailable, row, col) = await task;
                if (isAvailable)
                {
                    options.Add($"{world.Name}/{row}/{col}");
                }
            }

            if (options.Count == 0)
            {
                // No moves are available, so stay put.
                return;
            }

            // Leave the old tile.
            await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column}").Leaves(this);

            // Move to the next tile.
            var nextTileGrainId = options[new Random().Next(0, options.Count)];
            Logger.LogInformation($"{this.GetPrimaryKeyString()}'s next Tile ID is {nextTileGrainId}.");
            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
            await SetLocation(nextTileGrain);

            static async Task<(bool, int, int)> IsMoveAvailable(IGrainFactory grainFactory, World world, int row, int col)
            {
                // Don't allow out-of-bounds moves.
                if (col < 0 || col >= world.Columns || row < 0 || row >= world.Rows)
                {
                    return (false, 0, 0);
                }

                // Check if the new tile would be available.
                var tile = await grainFactory.GetGrain<ITileGrain>($"{world.Name}/{row}/{col}").GetTile();
                return (tile.Type == TileType.Space, row, col);
            }
        }
    }
}

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

            // can the wanderer move north?
            var isTileNorthOfMeAvailable = async () =>
            {
                if (!(Wanderer.State.CurrentLocation.Row > 0)) return false;
                var tileNorth = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row - 1}/{Wanderer.State.CurrentLocation.Column}").GetTile();
                return tileNorth.Type == TileType.Space;
            };

            // can the wanderer move west?
            var isTileWestOfMeAvailable = async () =>
            {
                if (!(Wanderer.State.CurrentLocation.Column > 0)) return false;
                var tileWest = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column - 1}").GetTile();
                return tileWest.Type == TileType.Space;
            };

            // can the wanderer move north?
            var isTileSouthOfMeAvailable = async () =>
            {
                if (!(Wanderer.State.CurrentLocation.Row < world.Rows - 1)) return false;
                var tileSouth = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row + 1}/{Wanderer.State.CurrentLocation.Column}").GetTile();
                return tileSouth.Type == TileType.Space;
            };

            // can the wanderer move east?
            var isTileEastOfMeAvailable = async () =>
            {
                if (!(Wanderer.State.CurrentLocation.Column < world.Columns - 1)) return false;
                var tileEast = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column + 1}").GetTile();
                return tileEast.Type == TileType.Space;
            };

            // save up the list of available options for our next direction
            var options = new List<string>();

            if (await isTileNorthOfMeAvailable())
            {
                int rowUp = Wanderer.State.CurrentLocation.Row - 1;
                options.Add($"{world.Name}/{rowUp}/{Wanderer.State.CurrentLocation.Column}");
            }
            if (await isTileWestOfMeAvailable())
            {
                int colLeft = Wanderer.State.CurrentLocation.Column - 1;
                options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colLeft}");
            }
            if (await isTileSouthOfMeAvailable())
            {
                int rowDown = Wanderer.State.CurrentLocation.Row + 1;
                options.Add($"{world.Name}/{rowDown}/{Wanderer.State.CurrentLocation.Column}");
            }
            if (await isTileEastOfMeAvailable())
            {
                int colRight = Wanderer.State.CurrentLocation.Column + 1;
                options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colRight}");
            }

            // leave the old tile
            await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column}").Leaves(this);

            // move to the next tile
            var nextTileGrainId = options[new Random().Next(0, options.Count)];
            Logger.LogInformation($"{this.GetPrimaryKeyString()}'s next Tile ID is {nextTileGrainId}.");
            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
            await SetLocation(nextTileGrain);
        }
    }
}

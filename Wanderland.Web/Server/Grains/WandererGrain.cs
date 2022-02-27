using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class WandererGrain : Grain, IWanderGrain
        // , IRemindable
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
            return Task.FromResult(Wanderer.State);
        }

        public override async Task OnActivateAsync()
        {
            RegisterTimer(async _ =>
                {
                    await Wander();
                }, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));

            await base.OnActivateAsync();
        }

        public async Task SetLocation(ITileGrain tileGrain)
        {
            Wanderer.State.CurrentLocation = await tileGrain.GetTile();
            await tileGrain.Arrives(this);
        }

        public async Task Wander()
        {
            var world = await GrainFactory.GetGrain<IWorldGrain>(Wanderer.State.CurrentLocation.World).GetWorld();

            bool canMoveUp = (Wanderer.State.CurrentLocation.Row > 0);
            bool canMoveRight = (Wanderer.State.CurrentLocation.Column < (world.Columns - 1));
            bool canMoveDown = (Wanderer.State.CurrentLocation.Row < (world.Rows - 1));
            bool canMoveLeft = (Wanderer.State.CurrentLocation.Column > 0);
            var currentTileGrain = GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column}");
            var options = new List<string>();

            if (canMoveUp)
            {
                int rowUp = Wanderer.State.CurrentLocation.Row - 1;
                options.Add($"{world.Name}/{rowUp}/{Wanderer.State.CurrentLocation.Column}");
            } 
            if(canMoveRight)
            {
                int colRight = Wanderer.State.CurrentLocation.Column + 1;
                options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colRight}");
            }
            if (canMoveDown)
            {
                int rowDown = Wanderer.State.CurrentLocation.Row + 1;
                options.Add($"{world.Name}/{rowDown}/{Wanderer.State.CurrentLocation.Column}");
            }
            if (canMoveLeft)
            {
                int colLeft = Wanderer.State.CurrentLocation.Column - 1;
                options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colLeft}");
            }

            var nextTileGrainId = options[new Random().Next(0, options.Count - 1)];

            Logger.LogInformation($"{this.GetPrimaryKeyString()}'s next Tile ID is {nextTileGrainId}.");
            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
            await SetLocation(nextTileGrain);
            await currentTileGrain.Leaves(this);
        }
    }
}

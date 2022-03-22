using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class WandererGrain : Grain, IWandererGrain
    {
        public WandererGrain([PersistentState(Constants.PersistenceKeys.WandererStateName, Constants.PersistenceKeys.WandererStorageName)]
            IPersistentState<Wanderer> wanderer, ILogger<WandererGrain> logger)
        {
            Wanderer = wanderer;
            Logger = logger;
        }

        public IPersistentState<Wanderer> Wanderer { get; }
        public ILogger<WandererGrain> Logger { get; }
        public World World { get; set; }

        private TimeSpan GetMoveDuration()
        {
            return TimeSpan.FromMilliseconds(Wanderer.State.Speed);
        }

        IDisposable _timer;
        private void ResetWanderTimer()
        {
            _timer?.Dispose();
            _timer = RegisterTimer(async _ =>
            {
                try
                {
                    await Wander();
                }
                catch
                {
                    _timer?.Dispose();
                    // swallowing this exception because this could 
                    // mean this wanderer's world and tiles have
                    // been wiped and they're still hanging out.
                }
            }, null, GetMoveDuration(), GetMoveDuration());
        }

        public override async Task OnActivateAsync()
        {
            ResetWanderTimer();
            await base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _timer?.Dispose();
            return base.OnDeactivateAsync();
        }

        public Task<Wanderer> GetWanderer()
        {
            Wanderer.State.Name = this.GetPrimaryKeyString();
            return Task.FromResult(Wanderer.State);
        }

        public async virtual Task SetInfo(Wanderer wanderer)
        {
            Wanderer.State = wanderer;
            await Wanderer.WriteStateAsync();
            ResetWanderTimer();
        }

        public virtual async Task SetLocation(ITileGrain tileGrain)
        {
            Wanderer.State.Name = this.GetPrimaryKeyString();
            Wanderer.State.CurrentLocation = await tileGrain.GetTile();
            await tileGrain.Arrives(Wanderer.State);
        }

        public async void Dispose()
        {
            _timer.Dispose();

            var lobbyGrain = GrainFactory.GetGrain<ILobbyGrain>(Guid.Empty);
            await lobbyGrain.JoinLobby(Wanderer.State);
        }

        public virtual Task SpeedUp(int ratio)
        {
            Wanderer.State.Speed = Wanderer.State.Speed - (Wanderer.State.Speed / ratio);
            ResetWanderTimer();
            return Task.CompletedTask;
        }

        public virtual Task SlowDown(int ratio)
        {
            Wanderer.State.Speed = Wanderer.State.Speed + (Wanderer.State.Speed / ratio);
            ResetWanderTimer();
            return Task.CompletedTask;
        }

        public async Task Wander()
        {
            var currentTile = Wanderer.State.CurrentLocation;

            if (currentTile != null)
            {
                World = await GrainFactory.GetGrain<IWorldGrain>(currentTile.World).GetWorld();
                if (World == null)
                {
                    Dispose();
                }
                else
                {
                    // save up the list of available options for our next direction
                    var options = new List<Func<Task>>();
                    if (await CanGoWest()) options.Add(new Func<Task>(GoWest));
                    if (await CanGoNorth()) options.Add(new Func<Task>(GoNorth));
                    if (await CanGoSouth()) options.Add(new Func<Task>(GoSouth));
                    if (await CanGoEast()) options.Add(new Func<Task>(GoEast));

                    if (options.Any())
                    {
                        // leave the old tile
                        var tileGrainId = $"{World.Name}/{currentTile.Row}/{currentTile.Column}";
                        await GrainFactory.GetGrain<ITileGrain>(tileGrainId).Leaves(Wanderer.State);

                        // move to the next tile
                        var nextMove = options[new Random().Next(0, options.Count)];
                        await nextMove();
                    }
                }
            }
        }

        public virtual async Task<bool> CanGoWest()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            if (!(currentTile.Column > 0)) return false;
            var tileWest = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{currentTile.Row}/{currentTile.Column - 1}").GetTile();
            return tileWest.Type == TileType.Space;
        }

        public virtual async Task GoWest()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            int colLeft = currentTile.Column - 1;
            var tileGrainName = $"{World.Name}/{currentTile.Row}/{colLeft}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoNorth()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            if (!(currentTile.Row > 0)) return false;
            var tileNorth = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{currentTile.Row - 1}/{currentTile.Column}").GetTile();
            return tileNorth.Type == TileType.Space;
        }

        public virtual async Task GoNorth()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            int rowUp = currentTile.Row - 1;
            var tileGrainName = $"{World.Name}/{rowUp}/{currentTile.Column}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoSouth()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            if (!(currentTile.Row < World.Rows - 1)) return false;
            var tileSouth = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{currentTile.Row + 1}/{currentTile.Column}").GetTile();
            return tileSouth.Type == TileType.Space;
        }

        public virtual async Task GoSouth()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            int rowDown = currentTile.Row + 1;
            var tileGrainName = $"{World.Name}/{rowDown}/{currentTile.Column}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoEast()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            if (!(currentTile.Column < World.Columns - 1)) return false;
            var tileEast = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{currentTile.Row}/{currentTile.Column + 1}").GetTile();
            return tileEast.Type == TileType.Space;
        }

        public virtual async Task GoEast()
        {
            var currentTile = Wanderer.State.CurrentLocation;
            int colRight = currentTile.Column + 1;
            var tileGrainName = $"{World.Name}/{currentTile.Row}/{colRight}";
            await Go(tileGrainName);
        }

        private async Task Go(string tileGrainName)
        {
            var currentTile = Wanderer.State.CurrentLocation;

            // leave the old tile
            var tileGrainId = $"{World.Name}/{currentTile.Row}/{currentTile.Column}";
            await GrainFactory.GetGrain<ITileGrain>(tileGrainId).Leaves(Wanderer.State);

            // move to the next tile
            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(tileGrainName);
            await SetLocation(nextTileGrain);
        }
    }
}

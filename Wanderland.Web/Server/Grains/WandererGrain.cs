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

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            ResetWanderTimer();
            await base.OnActivateAsync(cancellationToken);
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return base.OnDeactivateAsync(reason, cancellationToken);
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
            var tile = await tileGrain.GetTile();
            Wanderer.State.Name = this.GetPrimaryKeyString();
            await tileGrain.Arrives(Wanderer.State);
        }

        public async void Dispose()
        {
            _timer.Dispose();

            if(Wanderer.State.GetType().Equals(typeof(Wanderer))) // don't put monsters back in
            {
                var lobbyGrain = GrainFactory.GetGrain<ILobbyGrain>(Guid.Empty);
                await lobbyGrain.JoinLobby(Wanderer.State);
            }
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
            if (!(Wanderer.State.Location.Column > 0)) return false;
            var tileWest = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column - 1}").GetTile();
            return tileWest.Type == TileType.Space;
        }

        public virtual async Task GoWest()
        {
            var tileGrainName = $"{World.Name}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column - 1}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoNorth()
        {
            if (!(Wanderer.State.Location.Row > 0)) return false;
            var tileNorth = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{Wanderer.State.Location.Row - 1}/{Wanderer.State.Location.Column}").GetTile();
            return tileNorth.Type == TileType.Space;
        }

        public virtual async Task GoNorth()
        {
            int rowUp = Wanderer.State.Location.Row - 1;
            var tileGrainName = $"{World.Name}/{rowUp}/{Wanderer.State.Location.Column}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoSouth()
        {
            if (!(Wanderer.State.Location.Row < World.Rows - 1)) return false;
            var tileSouth = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{Wanderer.State.Location.Row + 1}/{Wanderer.State.Location.Column}").GetTile();
            return tileSouth.Type == TileType.Space;
        }

        public virtual async Task GoSouth()
        {
            var tileGrainName = $"{World.Name}/{Wanderer.State.Location.Row + 1}/{Wanderer.State.Location.Column}";
            await Go(tileGrainName);
        }

        public virtual async Task<bool> CanGoEast()
        {
            if (!(Wanderer.State.Location.Column < World.Columns - 1)) return false;
            var tileEast = await GrainFactory.GetGrain<ITileGrain>($"{World.Name}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column + 1}").GetTile();
            return tileEast.Type == TileType.Space;
        }

        public virtual async Task GoEast()
        {
            var tileGrainName = $"{World.Name}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column + 1}";
            await Go(tileGrainName);
        }

        private async Task Go(string tileGrainName)
        {
            // leave the old tile
            var tileGrainId = $"{World.Name}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column}";
            await GrainFactory.GetGrain<ITileGrain>(tileGrainId).Leaves(Wanderer.State);

            // move to the next tile
            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(tileGrainName);
            await SetLocation(nextTileGrain);
        }
    }
}

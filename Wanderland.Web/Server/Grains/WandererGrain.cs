﻿using Orleans;
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

        public virtual async Task SetLocation(ITileGrain tileGrain)
        {
            Wanderer.State.Name = this.GetPrimaryKeyString();
            Wanderer.State.CurrentLocation = await tileGrain.GetTile();
            await tileGrain.Arrives(this);
        }

        public async Task Wander()
        {
            if (Wanderer.State.CurrentLocation != null)
            {
                var world = await GrainFactory.GetGrain<IWorldGrain>(Wanderer.State.CurrentLocation.World).GetWorld();
                if (world == null)
                {
                    Dispose();
                }
                else
                {
                    // can the wanderer move north?
                    var isTileNorthOfMeAvailable = async () =>
                    {
                        if (Wanderer.State.CurrentLocation == null || !(Wanderer.State.CurrentLocation.Row > 0)) return false;
                        var tileNorth = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row - 1}/{Wanderer.State.CurrentLocation.Column}").GetTile();
                        return tileNorth.Type == TileType.Space;
                    };

                    // can the wanderer move west?
                    var isTileWestOfMeAvailable = async () =>
                    {
                        if (Wanderer.State.CurrentLocation == null || !(Wanderer.State.CurrentLocation.Column > 0)) return false;
                        var tileWest = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column - 1}").GetTile();
                        return tileWest.Type == TileType.Space;
                    };

                    // can the wanderer move north?
                    var isTileSouthOfMeAvailable = async () =>
                    {
                        if(Wanderer.State.CurrentLocation == null || !(Wanderer.State.CurrentLocation.Row < world.Rows - 1)) return false;
                        var tileSouth = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row + 1}/{Wanderer.State.CurrentLocation.Column}").GetTile();
                        return tileSouth.Type == TileType.Space;
                    };

                    // can the wanderer move east?
                    var isTileEastOfMeAvailable = async () =>
                    {
                        if (Wanderer.State.CurrentLocation == null || !(Wanderer.State.CurrentLocation.Column < world.Columns - 1)) return false;
                        var tileEast = await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column + 1}").GetTile();
                        return tileEast.Type == TileType.Space;
                    };

                    // save up the list of available options for our next direction
                    var options = new List<string>();

                    if (await isTileNorthOfMeAvailable() && Wanderer.State.CurrentLocation != null)
                    {
                        int rowUp = Wanderer.State.CurrentLocation.Row - 1;
                        options.Add($"{world.Name}/{rowUp}/{Wanderer.State.CurrentLocation.Column}");
                    }
                    if (await isTileWestOfMeAvailable() && Wanderer.State.CurrentLocation != null)
                    {
                        int colLeft = Wanderer.State.CurrentLocation.Column - 1;
                        options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colLeft}");
                    }
                    if (await isTileSouthOfMeAvailable() && Wanderer.State.CurrentLocation != null)
                    {
                        int rowDown = Wanderer.State.CurrentLocation.Row + 1;
                        options.Add($"{world.Name}/{rowDown}/{Wanderer.State.CurrentLocation.Column}");
                    }
                    if (await isTileEastOfMeAvailable() && Wanderer.State.CurrentLocation != null)
                    {
                        int colRight = Wanderer.State.CurrentLocation.Column + 1;
                        options.Add($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{colRight}");
                    }

                    if(options.Any())
                    {
                        if(Wanderer.State.CurrentLocation != null)
                        {
                            // leave the old tile
                            await GrainFactory.GetGrain<ITileGrain>($"{world.Name}/{Wanderer.State.CurrentLocation.Row}/{Wanderer.State.CurrentLocation.Column}").Leaves(this);

                            // move to the next tile
                            var nextTileGrainId = options[new Random().Next(0, options.Count)];
                            var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(nextTileGrainId);
                            await SetLocation(nextTileGrain);
                        }
                    }
                }
            }
        }

        public async virtual Task SetInfo(Wanderer wanderer)
        {
            Wanderer.State = wanderer;
            await Wanderer.WriteStateAsync();
            ResetWanderTimer();
        }

        public void Dispose()
        {
            _timer.Dispose();
            Wanderer.ClearStateAsync();
        }

        public Task SpeedUp(int ratio)
        {
            Wanderer.State.Speed = Wanderer.State.Speed - (Wanderer.State.Speed / ratio);
            ResetWanderTimer();
            return Task.CompletedTask;
        }

        public Task SlowDown(int ratio)
        {
            Wanderer.State.Speed = Wanderer.State.Speed + (Wanderer.State.Speed / ratio);
            ResetWanderTimer();
            return Task.CompletedTask;
        }
    }
}

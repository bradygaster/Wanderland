using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains;

[CollectionAgeLimit(Minutes = 10)]
public class WandererGrain : Grain, IWandererGrain
{
    public WandererGrain(
        [PersistentState(
            stateName: Constants.PersistenceKeys.WandererStateName, 
            storageName: Constants.PersistenceKeys.WandererStorageName)]
        IPersistentState<Wanderer> wanderer, ILogger<WandererGrain> logger,
        IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHubContext)
    {
        Wanderer = wanderer;
        Logger = logger;
        WanderlandHubContext = wanderlandHubContext;
    }

    public IPersistentState<Wanderer> Wanderer { get; }
    public ILogger<WandererGrain> Logger { get; }
    public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHubContext { get; }

    private TimeSpan GetMoveDuration()
    {
        return TimeSpan.FromMilliseconds(Wanderer.State.Speed);
    }

    IDisposable _timer;
    private void ResetWanderTimer()
    {
        _timer?.Dispose();
        _timer = RegisterTimer(async _ => await Wander(), null, GetMoveDuration(), GetMoveDuration());
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

        if(Wanderer.State.Health == WandererHealthState.Healthy)
        {
            ResetWanderTimer();
        }

        if (Wanderer.State.Health == WandererHealthState.Dead)
        {
            _timer?.Dispose();
        }
    }

    public virtual async Task SetLocation(ITileGrain tileGrain)
    {
        var tile = await tileGrain.GetTile();

        Wanderer.State.Name = this.GetPrimaryKeyString();
        Wanderer.State.Location.World = tile.World;
        Wanderer.State.Location.Row = tile.Row;
        Wanderer.State.Location.Column = tile.Column;

        await tileGrain.Arrives(Wanderer.State);

        ResetWanderTimer();
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
        // save up the list of available options for our next direction
        var options = new List<Func<Task>>();
        if (await CanGoWest()) options.Add(GoWest);
        if (await CanGoNorth()) options.Add(GoNorth);
        if (await CanGoSouth()) options.Add(GoSouth);
        if (await CanGoEast()) options.Add(GoEast);

        if (options.Any())
        {
            // leave the old tile
            var tileGrainId = $"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column}";
            await GrainFactory.GetGrain<ITileGrain>(tileGrainId).Leaves(Wanderer.State);

            // move to the next tile
            var nextMove = options[Random.Shared.Next(0, options.Count)];
            await nextMove();
        }
    }

    public virtual async Task<bool> CanGoWest()
    {
        if (!(Wanderer.State.Location.Column > 0)) return false;
        var tileWest = await GrainFactory.GetGrain<ITileGrain>($"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column - 1}").GetTile();
        return tileWest.Type is TileType.Space;
    }

    public virtual async Task GoWest()
    {
        var tileGrainName = $"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column - 1}";
        await Go(tileGrainName);
    }

    public virtual async Task<bool> CanGoNorth()
    {
        if (!(Wanderer.State.Location.Row > 0)) return false;
        var tileNorth = await GrainFactory.GetGrain<ITileGrain>($"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row - 1}/{Wanderer.State.Location.Column}").GetTile();
        return tileNorth.Type is TileType.Space;
    }

    public virtual async Task GoNorth()
    {
        int rowUp = Wanderer.State.Location.Row - 1;
        var tileGrainName = $"{Wanderer.State.Location.World}/{rowUp}/{Wanderer.State.Location.Column}";
        await Go(tileGrainName);
    }

    public virtual async Task<bool> CanGoSouth()
    {
        var world = await GrainFactory.GetGrain<IWorldGrain>(Wanderer.State.Location.World).GetWorld();
        if (!(Wanderer.State.Location.Row < world.Rows - 1)) return false;
        var tileSouth = await GrainFactory.GetGrain<ITileGrain>($"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row + 1}/{Wanderer.State.Location.Column}").GetTile();
        return tileSouth.Type == TileType.Space;
    }

    public virtual async Task GoSouth()
    {
        var tileGrainName = $"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row + 1}/{Wanderer.State.Location.Column}";
        await Go(tileGrainName);
    }

    public virtual async Task<bool> CanGoEast()
    {
        var world = await GrainFactory.GetGrain<IWorldGrain>(Wanderer.State.Location.World).GetWorld();
        if (!(Wanderer.State.Location.Column < world.Columns - 1)) return false;
        var tileEast = await GrainFactory.GetGrain<ITileGrain>($"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column + 1}").GetTile();
        return tileEast.Type is TileType.Space;
    }

    public virtual async Task GoEast()
    {
        var tileGrainName = $"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column + 1}";
        await Go(tileGrainName);
    }

    private async Task Go(string tileGrainName)
    {
        // leave the old tile
        var tileGrainId = $"{Wanderer.State.Location.World}/{Wanderer.State.Location.Row}/{Wanderer.State.Location.Column}";
        await GrainFactory.GetGrain<ITileGrain>(tileGrainId).Leaves(Wanderer.State);

        // move to the next tile
        var nextTileGrain = GrainFactory.GetGrain<ITileGrain>(tileGrainName);
        await SetLocation(nextTileGrain);
    }

    public async ValueTask OnDestroyWorld()
    {
        if (Wanderer.State.GetType().Equals(typeof(Wanderer))) // don't put monsters back in
        {
            var lobbyGrain = GrainFactory.GetGrain<ILobbyGrain>(Guid.Empty);
            await lobbyGrain.JoinLobby(Wanderer.State);

            Wanderer.State.Health = WandererHealthState.Dead;
            await WanderlandHubContext.Clients.All.PlayerUpdated(
                new PlayerUpdatedEventArgs
                {
                    Player = Wanderer.State
                });
        }

        base.DeactivateOnIdle();
    }
}

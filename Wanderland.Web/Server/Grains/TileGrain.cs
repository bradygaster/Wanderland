using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    [CollectionAgeLimit(Minutes = 2)]
    public class TileGrain : Grain, ITileGrain
    {
        public IPersistentState<Tile> Tile { get; }
        public ILogger<TileGrain> Logger { get; }
        public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHubContext { get; }

        public TileGrain([PersistentState(Constants.PersistenceKeys.TileStateName, Constants.PersistenceKeys.TileStorageName)] 
            IPersistentState<Tile> tile,
            ILogger<TileGrain> logger,
            IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHubContext)
        {
            Tile = tile;
            Logger = logger;
            WanderlandHubContext = wanderlandHubContext;
        }

        async Task ITileGrain.Arrives(Thing thing)
        {
            if(!Tile.State.ThingsHere.Any(x => x.Name == thing.Name))
            {
                Tile.State.ThingsHere.Add(thing);
                await WanderlandHubContext.Clients.Group(Tile.State.World).TileUpdated(Tile.State);
                await GrainFactory.GetGrain<IWorldGrain>(Tile.State.World).SetTile(Tile.State);
            }
        }

        async Task ITileGrain.Leaves(Thing thing)
        {
            if (Tile.State.ThingsHere.Any(x => x.Name == thing.Name))
            {
                Tile.State.ThingsHere.RemoveAll(x => x.Name == thing.Name);
                await WanderlandHubContext.Clients.Group(Tile.State.World).TileUpdated(Tile.State);
                await GrainFactory.GetGrain<IWorldGrain>(Tile.State.World).SetTile(Tile.State);
            }
        }

        Task<Tile> ITileGrain.GetTile()
        {
            return Task.FromResult(Tile.State);
        }

        Task ITileGrain.SetTile(Tile tile)
        {
            Tile.State = tile;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            var thingsHere = Tile.State.ThingsHere;
            foreach (var thing in thingsHere)
            {
                if(thing as Monster != null)
                {
                    var monsterGrain = GrainFactory.GetGrain<IMonsterGrain>(thing.Name);
                    monsterGrain.Dispose();
                }
                else if (thing as Wanderer != null)
                {
                    var wandererGrain = GrainFactory.GetGrain<IWandererGrain>(thing.Name);
                    wandererGrain.Dispose();
                }
            }
            base.DeactivateOnIdle();
        }
    }
}

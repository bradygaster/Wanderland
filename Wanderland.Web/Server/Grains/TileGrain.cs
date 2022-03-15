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
            if(!Tile.State.WanderersHere.Any(x => x.Name == thing.Name))
            {
                Tile.State.WanderersHere.Add(thing);
                await WanderlandHubContext.Clients.Group(Tile.State.World).TileUpdated(Tile.State);
            }
        }

        async Task ITileGrain.Leaves(Thing thing)
        {
            if (Tile.State.WanderersHere.Any(x => x.Name == thing.Name))
            {
                Tile.State.WanderersHere.RemoveAll(x => x.Name == thing.Name);
                await WanderlandHubContext.Clients.Group(Tile.State.World).TileUpdated(Tile.State);
            }
        }

        Task<Tile> ITileGrain.GetTile()
        {
            return Task.FromResult(Tile.State);
        }

        Task ITileGrain.SetTileInfo(Tile tile)
        {
            Tile.State = tile;
            return Task.CompletedTask;
        }
    }
}

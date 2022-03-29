using Microsoft.AspNetCore.SignalR;
using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Server.Hubs;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains;

public class LobbyGrain : Grain, ILobbyGrain
{
    private IPersistentState<Lobby> Lobby {  get; set; }
    public IHubContext<WanderlandHub, IWanderlandHubClient> WanderlandHubContext { get; }

    public LobbyGrain(
        [PersistentState(
            stateName: Constants.PersistenceKeys.LobbyStateName, 
            storageName: Constants.PersistenceKeys.LobbyStorageName)]
        IPersistentState<Lobby> lobby, 
        IHubContext<WanderlandHub, IWanderlandHubClient> wanderlandHubContext)
    {
        Lobby = lobby;
        WanderlandHubContext = wanderlandHubContext;
    }

    public async Task<List<Wanderer>> GetPlayersInLobbby()
    {
        await Lobby.ReadStateAsync();
        return Lobby.State.Wanderers;
    }

    public async Task JoinLobby(Wanderer wanderer)
    {
        wanderer.Health = WandererHealthState.Healthy;
        Lobby.State.Wanderers.Add(wanderer);
        await Lobby.WriteStateAsync();
    }

    public async Task LeaveLobby(Wanderer wanderer)
    {
        Lobby.State.Wanderers.RemoveAll(x => x.Name.Equals(wanderer.Name, StringComparison.OrdinalIgnoreCase));
        await Lobby.WriteStateAsync();
    }

    async Task<List<Wanderer>> ILobbyGrain.GetPlayersForNextWorld()
    {
        if (Lobby.State.Wanderers.Count <= 10)
        {
            Lobby.State.CreateFakeData();
        }

        var tmp = Lobby.State.Wanderers.Take(10).ToList();
        tmp.ForEach(_ => Lobby.State.Wanderers.RemoveAll(w => w.Name.Equals(_.Name, StringComparison.OrdinalIgnoreCase)));
        await Lobby.WriteStateAsync();
        await PlayersForNextWorldChosen(tmp);
        return tmp;
    }

    public async Task PlayersForNextWorldChosen(List<Wanderer> wanderers)
    {
        await WanderlandHubContext.Clients.All.PlayerListUpdated(new PlayerListUpdatedEventArgs
        {
            Players = wanderers
        });
    }
}

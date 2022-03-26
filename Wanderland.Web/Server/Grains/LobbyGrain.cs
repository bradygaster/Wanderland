using Orleans;
using Orleans.Runtime;
using Wanderland.Web.Shared;

namespace Wanderland.Web.Server.Grains
{
    public class LobbyGrain : Grain, ILobbyGrain
    {
        private IPersistentState<Lobby> Lobby {  get; set; }

        public LobbyGrain([PersistentState(Constants.PersistenceKeys.LobbyStateName, Constants.PersistenceKeys.LobbyStorageName)]
            IPersistentState<Lobby> lobby)
        {
            Lobby = lobby;
        }

        public async Task<List<Wanderer>> GetPlayersForNextWorld()
        {
            if(Lobby.State.Wanderers.Count <= 10)
            {
                Lobby.State.CreateFakeData();
            }

            var tmp = Lobby.State.Wanderers.Take(10).ToList();
            tmp.ForEach(_ => Lobby.State.Wanderers.RemoveAll(w => w.Name.Equals(_.Name, StringComparison.OrdinalIgnoreCase)));
            await Lobby.WriteStateAsync();
            return tmp;
        }

        public async Task<List<Wanderer>> GetPlayersInLobbby()
        {
            await Lobby.ReadStateAsync();
            return Lobby.State.Wanderers;
        }

        public async Task JoinLobby(Wanderer wanderer)
        {
            Lobby.State.Wanderers.Add(wanderer);
            await Lobby.WriteStateAsync();
        }

        public async Task LeaveLobby(Wanderer wanderer)
        {
            Lobby.State.Wanderers.RemoveAll(x => x.Name.Equals(wanderer.Name, StringComparison.OrdinalIgnoreCase));
            await Lobby.WriteStateAsync();
        }

        public Task WorldReady(World world)
        {
            return Task.CompletedTask;
        }
    }
}

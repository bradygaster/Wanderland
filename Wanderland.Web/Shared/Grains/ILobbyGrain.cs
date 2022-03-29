using Orleans;

namespace Wanderland.Web.Shared;

public interface ILobbyGrain : IGrainWithGuidKey
{
    Task JoinLobby(Wanderer wanderer);
    Task LeaveLobby(Wanderer wanderer);
    Task<List<Wanderer>> GetPlayersInLobbby();
    Task<List<Wanderer>> GetPlayersForNextWorld();
    Task PlayersForNextWorldChosen(List<Wanderer> wanderers);
}

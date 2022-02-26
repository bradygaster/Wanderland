using Refit;

namespace Wanderland.Web.Shared
{
    public interface IWanderlandHttpApiClient
    {
        [Get("/worlds")]
        Task<List<World>> GetWorlds();

        [Get("/worlds/{name}")]
        Task<World> GetWorld(string name);

        [Get("/worlds/{name}/tiles")]
        Task<List<Tile>> GetWorldTiles(string name);
    }

    public class WanderlandHttpApiClient : IWanderlandHttpApiClient
    {
        HttpClient _httpClient;

        public WanderlandHttpApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        async Task<World> IWanderlandHttpApiClient.GetWorld(string name)
        {
            return await RestService.For<IWanderlandHttpApiClient>(_httpClient).GetWorld(name);
        }

        async Task<List<World>> IWanderlandHttpApiClient.GetWorlds()
        {
            return await RestService.For<IWanderlandHttpApiClient>(_httpClient).GetWorlds();
        }

        async Task<List<Tile>> IWanderlandHttpApiClient.GetWorldTiles(string name)
        {
            return await RestService.For<IWanderlandHttpApiClient>(_httpClient).GetWorldTiles(name);
        }
    }
}

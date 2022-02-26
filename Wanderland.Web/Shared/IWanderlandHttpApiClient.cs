using Refit;

namespace Wanderland.Web.Shared
{
    public interface IWanderlandHttpApiClient
    {
        [Get("/worlds")]
        Task<List<World>> GetWorlds();
    }

    public class WanderlandHttpApiClient : IWanderlandHttpApiClient
    {
        HttpClient _httpClient;

        public WanderlandHttpApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<World>> GetWorlds()
        {
            return await RestService.For<IWanderlandHttpApiClient>(_httpClient).GetWorlds();
        }
    }
}

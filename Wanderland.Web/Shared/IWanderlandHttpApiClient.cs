using Refit;

namespace Wanderland.Web.Shared;

public interface IWanderlandHttpApiClient
{
    [Delete("/worlds/{name}")]
    Task DeleteWorld(string name);

    [Get("/worlds")]
    Task<List<World>> GetWorlds();

    [Get("/worlds/{name}")]
    Task<World> GetWorld(string name);

    [Get("/worlds/{name}/tiles")]
    Task<List<Tile>> GetWorldTiles(string name);

    [Get("/worlds/{name}/tiles/{row}/{column}")]
    Task<Tile> GetTileCurrentState(string name, int row, int column);
}

public sealed class WanderlandHttpApiClient : IWanderlandHttpApiClient
{
    readonly HttpClient _httpClient;

    public WanderlandHttpApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DeleteWorld(string name)
    {
        await RestService.For<IWanderlandHttpApiClient>(_httpClient).DeleteWorld(name);
    }

    async Task<Tile> IWanderlandHttpApiClient.GetTileCurrentState(string name, int row, int column)
    {
        return await RestService.For<IWanderlandHttpApiClient>(_httpClient).GetTileCurrentState(name, row, column);
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

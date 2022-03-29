using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Lobby
{
    [Id(0)]
    public List<Wanderer> Wanderers { get; set; } = new List<Wanderer>();
}

public static class LobbyExtensions
{
    public static Lobby CreateFakeData(this Lobby lobby)
    {
        var names = new string[]
        {
            "Andy", "Bob", "Charlie", "Danielle", "Eduard", "Francis", "George", 
            "Harry", "Imogen", "Jack", "Karl", "Larry", "Michel", "Nancy", 
            "Ollie", "Phaedrus", "Quincy", "Ralph", "Stephanie", "Trista", 
            "Uma", "Victoria", "Wayne", "Xaxier", "Yancey", "Zeek"
        };

        for (int i = 0; i < names.Length; ++i)
        {
            lobby.Wanderers.Add(new Wanderer
            {
                Name = names[i]
            });
        }

        return lobby;
    }
}

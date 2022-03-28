using Orleans;

namespace Wanderland.Web.Shared
{
    [GenerateSerializer]
    public class Lobby
    {
        public List<Wanderer> Wanderers { get; set; } = new List<Wanderer>();
    }

    public static class LobbyExtensions
    {
        public static Lobby CreateFakeData(this Lobby lobby)
        {
            var names = new string[]
            {
                "Andy", "Bob", "Charlie", "Danielle", "Eduard", "Francis", "George", "Harry", "Imogen", "Jack", "Karl", "Larry", "Michel", "Nancy", "Ollie", "Phaedrus", "Quincy", "Ralph", "Stephanie", "Trista", "Uma", "Victoria", "Wayne", "Xaxier", "Yancey", "Zeek"
            };

            foreach (var c in names)
            {
                lobby.Wanderers.Add(new Wanderer
                {
                    Name = c
                });
            }

            return lobby;
        }
    }
}

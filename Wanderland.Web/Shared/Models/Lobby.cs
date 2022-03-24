namespace Wanderland.Web.Shared
{
    public class Lobby
    {
        public List<Wanderer> Wanderers { get; set; } = new List<Wanderer>();
    }

    public static class LobbyExtensions
    {
        public static Lobby CreateFakeData(this Lobby lobby)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            foreach (var c in chars)
            {
                lobby.Wanderers.Add(new Wanderer
                {
                    Name = c.ToString()
                });
            }

            return lobby;
        }
    }
}

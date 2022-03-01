namespace Wanderland.Web.Shared
{
    public class Wanderer
    {
        public string Name { get; set; } = string.Empty;
        public Tile CurrentLocation { get; set; }
        public string AvatarImage { get; set; }
    }
}

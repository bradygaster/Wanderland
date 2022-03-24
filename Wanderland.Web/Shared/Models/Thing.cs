namespace Wanderland.Web.Shared
{
    public class Thing
    {
        public string Name { get; set; } = string.Empty;

        public Tile CurrentLocation { get; set; }

        public string? AvatarImageUrl { get; set; } = null;
    }
}

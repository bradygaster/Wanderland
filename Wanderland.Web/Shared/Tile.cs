namespace Wanderland.Web.Shared
{
    public class Tile
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public TileType Type { get; set; } = TileType.Space;
    }

    public enum TileType
    {
        Space = 0,
        Barrier = 1
    }
}

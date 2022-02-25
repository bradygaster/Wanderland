namespace Wanderland.Web.Shared
{
    public class Tile
    {
        public Coordinate Coordinate { get; set; }
        public TileType Type { get; set; } = TileType.Space;
    }

    public enum TileType
    {
        Space = 0,
        Barrier = 1
    }
}

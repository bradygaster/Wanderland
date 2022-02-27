namespace Wanderland.Web.Shared
{
    public class Tile
    {
        public string World { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public TileType Type { get; set; } = TileType.Space;
        public List<IWanderGrain> WanderersHere { get; set; } = new List<IWanderGrain>();
    }

    public enum TileType
    {
        Space = 0,
        Barrier = 1
    }
}

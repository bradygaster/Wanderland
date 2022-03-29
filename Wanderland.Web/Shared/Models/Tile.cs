using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Tile
{
    [Id(0)]
    public string World { get; set; }

    [Id(1)]
    public int Row { get; set; }

    [Id(2)]
    public int Column { get; set; }

    [Id(3)]
    public TileType Type { get; set; } = TileType.Space;

    [Id(4)]
    public List<Thing> ThingsHere { get; set; } = new();
}

[GenerateSerializer]
public enum TileType
{
    Space = 0,
    Barrier = 1
}

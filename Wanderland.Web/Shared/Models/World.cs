using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class World
{
    [Id(0)]
    public int Columns { get; set; } = 0;

    [Id(1)]
    public int Rows { get; set; } = 0;

    [Id(2)]
    public string Name { get; set; } = string.Empty;

    [Id(3)]
    public DateTime Started { get; set; } = DateTime.Now;

    [Id(4)]
    public DateTime? Ended { get; set; } = null;

    [Id(5)]
    public List<Tile> Tiles { get; set; } = new();
}

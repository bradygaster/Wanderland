using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Coordinate
{
    [Id(0)]
    public int Row { get; set; } = 0;

    [Id(1)]
    public int Column { get; set; } = 0;

    [Id(2)]
    public string World { get; set; }
}

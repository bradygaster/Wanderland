using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Wanderer : Thing
{
    [Id(0)]
    public int Speed { get; set; } = 200;

    [Id(1)]
    public Coordinate Location { get; set; } = new();

    [Id(2)]
    public WandererHealthState Health { get; set; } = WandererHealthState.Healthy;
}

public enum WandererHealthState
{
    Dead = 0,
    Healthy = 16
}

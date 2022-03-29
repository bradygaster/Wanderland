using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class SystemStatusUpdate
{
    [Id(0)]
    public int GrainsActive { get; set; }

    [Id(1)]
    public int WorldsCompleted { get; set; }

    [Id(2)]
    public DateTime DateStarted { get; set; }

    [Id(3)]
    public TimeSpan TimeUp { get; set; }
}

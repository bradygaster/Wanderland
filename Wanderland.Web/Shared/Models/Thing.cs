using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Thing
{
    [Id(0)]
    public string Name { get; set; } = string.Empty;

    [Id(1)]
    public string? AvatarImageUrl { get; set; } = null;
}

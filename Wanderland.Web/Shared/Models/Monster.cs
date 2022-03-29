using Orleans;

namespace Wanderland.Web.Shared;

[GenerateSerializer]
public class Monster : Wanderer
{
    public Monster()
    {
        Speed = 1_000;
    }
}

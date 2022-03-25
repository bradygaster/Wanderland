using Orleans;

namespace Wanderland.Web.Shared
{
    [GenerateSerializer]
    public class Wanderer : Thing
    {
        [Id(0)]
        public int Speed { get; set; } = 1500;
    }
}

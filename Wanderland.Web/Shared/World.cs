namespace Wanderland.Web.Shared
{
    public class World
    {
        public int Columns { get; set; } = 0;
        public int Rows { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public DateTime Started { get; set; } = DateTime.UtcNow;
        public DateTime? Ended { get; set; } = null;
    }
}

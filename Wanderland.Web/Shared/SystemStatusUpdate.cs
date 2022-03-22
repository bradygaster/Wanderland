namespace Wanderland.Web.Shared
{
    public class SystemStatusUpdate
    {
        public int GrainsActive { get; set; }
        public int WorldsCompleted { get; set; }
        public DateTime DateStarted { get; set; }
        public TimeSpan TimeUp { get; set; }
    }
}

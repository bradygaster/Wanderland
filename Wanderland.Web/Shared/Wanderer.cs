namespace Wanderland.Web.Shared
{
    public class Wanderer
    {
        /// <summary>
        /// This wanderer's names. Wanderer names must be unique.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The current location of this wanderer.
        /// </summary>
        public Tile CurrentLocation { get; set; }

        /// <summary>
        /// Speed is the number of milliseconds between each wanderer's move.
        /// </summary>
        public int Speed { get; set; } = 1500;
    }
}

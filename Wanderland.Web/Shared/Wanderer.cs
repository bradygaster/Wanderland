namespace Wanderland.Web.Shared
{
    public class Wanderer
    {
        /// <summary>
        /// This wanderer's names. Wanderer names 
        /// must be unique per world.
        /// </summary>
        /// <remarks>
        /// Like, we really haven't tested what happens
        /// if two Toms show up in anyspace.
        /// </remarks>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The current location of this wanderer.
        /// </summary>
        public Tile CurrentLocation { get; set; }

        /// <summary>
        /// Speed is the number of milliseconds between 
        /// each wanderer's move.
        /// </summary>
        public int Speed { get; set; } = 1500;

        /// <summary>
        /// If this wanderer has an image URL representing them, 
        /// use it here. 
        /// </summary>
        public string? AvatarImageUrl { get; set; } = null;

        public WandererState State { get; set; } = WandererState.Healthy;
    }

    public enum WandererState
    {
        Dead = -1,
        Healthy = 0
    }
}

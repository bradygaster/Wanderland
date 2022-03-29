namespace Wanderland.Web.Shared;

public interface IWander
{
    Task Wander();
    Task SetLocation(ITileGrain tileGrain);
    Task SpeedUp(int ratio);
    Task SlowDown(int ratio);
    Task<bool> CanGoWest();
    Task GoWest();
    Task<bool> CanGoNorth();
    Task GoNorth();
    Task<bool> CanGoSouth();
    Task GoSouth();
    Task<bool> CanGoEast();
    Task GoEast();
}

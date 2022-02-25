namespace Wanderland.Web.Shared
{
    public struct Coordinate
    {
        public Coordinate() : this(0, 0) { }

        public Coordinate(int row = 0, int column = 0)
        {
            Row = row;
            Column = column;
        }

        public int Row { get; set; }
        public int Column { get; set; }
    }
}

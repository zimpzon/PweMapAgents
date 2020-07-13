using Pwe.Shared;
using System.Collections.Generic;

namespace Pwe.OverpassTiles
{
    public class WayTileNode
    {
        public long Id { get; set; }
        public GeoCoord Point { get; set; }
        public List<long> Conn { get; set; } = new List<long>();
        public byte? Inside { get; set; } // Is node inside tile or not?
    }
}

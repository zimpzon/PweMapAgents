using System.Collections.Generic;

namespace Pwe.Shared
{
    public class MapAgentPath
    {
        public long PathMs { get; set; }
        public List<GeoCoord> Points { get; set; } = new List<GeoCoord>();
        public List<long> PointAbsTimestampMs { get; set; } = new List<long>();
        public List<long> TileIds { get; set; } = new List<long>();
        public string EncodedPolyline { get; set; }
    }
}

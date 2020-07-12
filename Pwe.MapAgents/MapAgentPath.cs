using Pwe.World;
using System.Collections.Generic;

namespace Pwe.MapAgents
{
    public class MapAgentPath
    {
        public long UnixMsStart { get; set; }
        public long UnixMsEnd { get; set; }
        public List<GeoCoord> Points { get; set; } = new List<GeoCoord>();
    }
}

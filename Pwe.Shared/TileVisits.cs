using System.Collections.Generic;

namespace Pwe.Shared
{
    public class NodeCount
    {
        public long NodeId { get; set; }
        public long Count { get; set; }
    }

    public class TileVisits
    {
        public long TileId { get; set; }
        public List<NodeCount> NodeCounts { get; set; } = new List<NodeCount>();
    }
}

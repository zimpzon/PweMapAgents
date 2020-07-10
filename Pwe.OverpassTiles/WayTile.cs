using System.Collections.Generic;

namespace Pwe.OverpassTiles
{
    public class WayTile
    {
        public long Id { get; set; }
        public List<WayTileNode> Nodes { get; set; } = new List<WayTileNode>();
    }
}

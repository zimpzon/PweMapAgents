﻿using Pwe.OverpassTiles;
using Pwe.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pwe.World
{
    public interface IGraphPeek
    {
        Task<(bool deadEndFound, bool unexploredNodeFound, long visitedNodesFound, long unvisitedNodesFound, long totalVisitCount, List<GeoCoord> explored)> Peek(WayTileNode root, WayTileNode first);
    }
}

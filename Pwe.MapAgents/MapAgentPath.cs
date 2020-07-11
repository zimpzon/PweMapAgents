using System.Collections.Generic;

namespace Pwe.MapAgents
{
    public class MapAgentPath
    {
        public long UnixMsStart { get; set; }
        public long UnixMsEnd { get; set; }
        public List<double> Points { get; set; }
    }
}

using System;

namespace Pwe.MapAgents
{
    public class MapAgent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double StartLat { get; set; }
        public double StartLon { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime LastUpdateUtc { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double TravelledMeters { get; set; }
    }
}

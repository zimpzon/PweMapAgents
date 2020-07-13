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
        public double MetersPerSecond { get; set; }
        public double TravelledMeters { get; set; }
    }
}

using System;

namespace Pwe.Shared
{
    /// <summary>
    /// Pinning ends when EndTime is reached or there are no more selfies to take (on some locations there are no street view images for selfies)
    /// </summary>
    public class Pin
    {
        public GeoCoord Center { get; set; }
        public double MaxDistanceMeters { get; set; }
        public DateTime TimeoutUtc { get; set; }
        public int SelfiesLeft { get; set; }
        public TimeSpan MinTimeBetweenSelfies { get; set; }
        public TimeSpan MaxTimeBetweenSelfies { get; set; }
        public DateTime NextSelfieTimeUtc { get; set; }
    }
}

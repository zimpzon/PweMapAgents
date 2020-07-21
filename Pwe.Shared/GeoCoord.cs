namespace Pwe.Shared
{
    public class GeoCoord
    {
        public GeoCoord() { }
        public GeoCoord(double lon, double lat)
        {
            Lon = lon;
            Lat = lat;
        }

        public override string ToString()
            => $"[{Lat:0.0000000}, {Lon:0.0000000}]";

        public double Lon { get; set; }
        public double Lat { get; set; }
    }
}

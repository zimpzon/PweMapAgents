namespace Pwe.World
{
    public class GeoCoord
    {
        public GeoCoord(double lon, double lat)
        {
            Lon = lon;
            Lat = lat;
        }

        public double Lon { get; set; }
        public double Lat { get; set; }
    }
}

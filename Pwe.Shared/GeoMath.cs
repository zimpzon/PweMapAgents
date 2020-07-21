using System;

namespace Pwe.Shared
{
    public static class GeoMath
    {
        static public long UnixMs()
            => (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

        public static double MetersDistanceTo(GeoCoord p0, GeoCoord p1)
            => MetersDistanceTo(p0.Lon, p0.Lat, p1.Lon, p1.Lat);

        static double DegToRad(double deg)
            => (deg * Math.PI / 180);

        static double RadToDeg(double rad)
         => (rad * 180 / Math.PI);

        static double Lerp(double a, double b, double t)
            => (b - a) * t + a;

        public static GeoCoord Interpolate(GeoCoord p0, GeoCoord p1, double t)
        {
            return new GeoCoord(Lerp(p0.Lon, p1.Lon, t), Lerp(p0.Lat, p1.Lat, t));
        }

        public static double CalculateBearing(GeoCoord startPoint, GeoCoord endPoint)
        {
            double lat1 = DegToRad(startPoint.Lat);
            double lat2 = DegToRad(endPoint.Lat);
            double deltaLon = DegToRad(endPoint.Lon - startPoint.Lon);

            double y = Math.Sin(deltaLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);
            double bearing = Math.Atan2(y, x);

            // since atan2 returns a value between -180 and +180, we need to convert it to 0 - 360 degrees
            return (RadToDeg(bearing) + 360) % 360;
        }

        public static double MetersDistanceTo(double lon0, double lat0, double lon1, double lat1)
        {
            double rlat1 = Math.PI * lat0 / 180;
            double rlat2 = Math.PI * lat1 / 180;
            double theta = lon0 - lon1;
            double rtheta = Math.PI * theta / 180;
            double dist =
                Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) *
                Math.Cos(rlat2) * Math.Cos(rtheta);
            dist = Math.Acos(dist);
            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;

            if (double.IsNaN(dist))
                dist = 0;

            return dist * 1000;
        }
    }
}

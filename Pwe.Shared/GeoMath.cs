using System;
using System.Threading.Tasks;

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

        public static async Task Line(int x, int y, int x2, int y2, Func<int, int, Task> setPixel)
        {
            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                await setPixel(x, y);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }
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

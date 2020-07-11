using System;

namespace Pwe.World
{
    public static class GeoMath
    {
        static public double MetersDistanceTo(double lat1, double lon1, double lat2, double lon2)
        {
            double rlat1 = Math.PI * lat1 / 180;
            double rlat2 = Math.PI * lat2 / 180;
            double theta = lon1 - lon2;
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

    // Google Polyline Encode
    //public static string Encode(IEnumerable<GeoLocation> points)
    //{
    //    var str = new StringBuilder();

    //    var encodeDiff = (Action<int>)(diff => {
    //        int shifted = diff << 1;
    //        if (diff < 0)
    //            shifted = ~shifted;
    //        int rem = shifted;
    //        while (rem >= 0x20)
    //        {
    //            str.Append((char)((0x20 | (rem & 0x1f)) + 63));
    //            rem >>= 5;
    //        }
    //        str.Append((char)(rem + 63));
    //    });

    //    int lastLat = 0;
    //    int lastLng = 0;
    //    foreach (var point in points)
    //    {
    //        int lat = (int)Math.Round(point.Lat * 1E5);
    //        int lng = (int)Math.Round(point.Lon * 1E5);
    //        encodeDiff(lat - lastLat);
    //        encodeDiff(lng - lastLng);
    //        lastLat = lat;
    //        lastLng = lng;
    //    }
    //    return str.ToString();
    //}

}

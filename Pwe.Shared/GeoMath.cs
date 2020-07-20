using System;
using System.Collections.Generic;

namespace Pwe.Shared
{
    public static class GeoMath
    {
        static public long UnixMs()
            => (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;

        static public double MetersDistanceTo(GeoCoord p0, GeoCoord p1)
            => MetersDistanceTo(p0.Lon, p0.Lat, p1.Lon, p1.Lat);

        static public double MetersDistanceTo(double lon0, double lat0, double lon1, double lat1)
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

    //public static GeoCoord LerpPath(List<GeoCoord> path, double t)
    //{
    //    var polyLineMs = msEnd - msBegin;

    //    if (t > 0.95 && !pathRequested)
    //    {
    //        console.log("Getting path, t = " + t);
    //        updatePath();
    //    }

    //    var distT = agent.PolyLineTotalLength * t;

    //    // Find the correct segment
    //    var i;
    //    for (i = agent.LastPathIdx; i < agent.PolyLineSummedDistances.length - 1; i++)
    //    {
    //        if (agent.PolyLineSummedDistances[i] > distT)
    //            break;
    //    }

    //    // i is now one too far.
    //    i--;
    //    agent.LastPathIdx = i;
    //    var seg0 = agent.PolyLine[i + 0];
    //    var seg1 = agent.PolyLine[i + 1];
    //    var t0 = agent.PolyLineSummedDistances[i + 0] / agent.PolyLineTotalLength;
    //    var t1 = agent.PolyLineSummedDistances[i + 1] / agent.PolyLineTotalLength;
    //    var segT = (t - t0) * 1 / (t1 - t0);
    //    var interpolated = google.maps.geometry.spherical.interpolate(seg0, seg1, segT);
    //    marker.setLatLng([interpolated.lat(), interpolated.lng()]);

    //    if (isFirstPathLoad)
    //    {
    //        isFirstPathLoad = false;
    //        mymap.setView([interpolated.lat(), interpolated.lng()], 14);
    //    }
    //}

}

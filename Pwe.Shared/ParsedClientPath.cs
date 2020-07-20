using System;
using System.Collections.Generic;
using System.Linq;

namespace Pwe.Shared
{
    public class ParsedClientPath
    {
        public double TotalLengthMeters { get; private set; }
        public TimeSpan PathTime { get; private set; }
        public long MsStart { get; set; }
        public long MsEnd { get; set; }
        public List<GeoCoord> Points { get; private set; } = new List<GeoCoord>();
        public List<double> SummedDistances { get; private set; } = new List<double>();

        public TimeSpan TimeRemaining { get; set; }
        public double T { get; set; }
        public GeoCoord Position { get; private set; }

        public void SetTime(long unixTimeMs)
        {
            TimeRemaining = TimeSpan.FromMilliseconds(Math.Max(MsEnd - unixTimeMs, 0));
            double t = 1.0 - (TimeRemaining.TotalMilliseconds / PathTime.TotalMilliseconds);
            T = Math.Clamp(t, 0.0, 1.0);
        }

        public static ParsedClientPath Create(AgentClientPath path)
        {
            var result = new ParsedClientPath();
            result.Points = GooglePolylineConverter.Decode(path.EncodedPolyline).ToList();
            result.MsStart = path.MsStart;
            result.MsEnd = path.MsEnd;
            result.PathTime = TimeSpan.FromMilliseconds(result.MsEnd - result.MsStart);

            result.SummedDistances.Add(0);
            double sumMeters = 0;
            for (int i = 0; i < result.Points.Count - 1; ++i)
            {
                double dist = GeoMath.MetersDistanceTo(result.Points[i], result.Points[i + 1]);
                sumMeters += dist;
                result.SummedDistances.Add(sumMeters);
            }
            result.TotalLengthMeters = sumMeters;
            return result;
        }
    }
}

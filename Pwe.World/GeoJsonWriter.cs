using Pwe.Shared;
using Pwe.World;
using System.Collections.Generic;
using System.Text.Json;

namespace Pwe.GeoJson
{
    public static class GeoJsonBuilder
    {
        public static string AgentPath(MapAgentPath path, bool addBoundingBoxex = true)
        {
            var result = new GeoJsonMultiLine();
            for(int i = 0; i < path.Points.Count - 2; i++)
            {
                var p0 = path.Points[i];
                var p1 = path.Points[i + 1];
                var segment = new List<List<double>>();
                segment.Add(new List<double> { p0.Lon, p0.Lat });
                segment.Add(new List<double> { p1.Lon, p1.Lat });
                result.coordinates.Add(segment);
            }

            if (addBoundingBoxex)
            {
                foreach (var tileId in path.TileIds)
                {
                    var bbox = new List<List<double>>();
                    var bounds = TileMath.GetTileBounds(tileId, WorldGraph.Zoom);
                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
                    bbox.Add(new List<double> { bounds.lon1, bounds.lat0 });
                    bbox.Add(new List<double> { bounds.lon1, bounds.lat1 });
                    bbox.Add(new List<double> { bounds.lon0, bounds.lat1 });
                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
                    result.coordinates.Add(bbox);
                }
            }

            return JsonSerializer.Serialize(result);
        }

        class GeoJsonGeometryCollection
        {
            public string type { get; } = "GeometryCollection";
            public List<object> geometries = new List<object>();
        }

        class GeoJsonMultiLine
        {
            public string type { get; } = "MultiLineString";
            public List<List<List<double>>> coordinates { get; set; } = new List<List<List<double>>>();
        }

        class GeoJsonMultiPoint
        {
            public string type { get; } = "MultiPoint";
            public List<List<double>> coordinates { get; set; } = new List<List<double>>();
        }
    }
}

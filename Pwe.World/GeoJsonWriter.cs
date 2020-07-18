using Pwe.Shared;
using Pwe.World;
using System.Collections.Generic;
using System.Text.Json;

namespace Pwe.GeoJson
{
    public static class GeoJsonBuilder
    {
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static string AgentPath(MapAgentPath path, bool addBoundingBoxex = true)
        {
            var result = new GeoJsonMultiLine();
            for(int i = 0; i < path.Points.Count - 2; i++)
            {
                var p0 = path.Points[i];
                var p1 = path.Points[i + 1];
                var segment = new List<List<double>>
                {
                    new List<double> { p0.Lon, p0.Lat },
                    new List<double> { p1.Lon, p1.Lat }
                };
                result.Coordinates.Add(segment);
            }

            if (addBoundingBoxex)
            {
                foreach (var tileId in path.TileIds)
                {
                    var bbox = new List<List<double>>();
                    var (lon0, lat0, lon1, lat1) = TileMath.GetTileBounds(tileId, WorldGraph.Zoom);
                    bbox.Add(new List<double> { lon0, lat0 });
                    bbox.Add(new List<double> { lon1, lat0 });
                    bbox.Add(new List<double> { lon1, lat1 });
                    bbox.Add(new List<double> { lon0, lat1 });
                    bbox.Add(new List<double> { lon0, lat0 });
                    result.Coordinates.Add(bbox);
                }
            }

            return JsonSerializer.Serialize(result, SerializerOptions);
        }

        class GeoJsonGeometryCollection
        {
            public string Type { get; } = "GeometryCollection";
            public List<object> Geometries = new List<object>();
        }

        class GeoJsonMultiLine
        {
            public string Type { get; } = "MultiLineString";
            public List<List<List<double>>> Coordinates { get; set; } = new List<List<List<double>>>();
        }

        class GeoJsonMultiPoint
        {
            public string Type { get; } = "MultiPoint";
            public List<List<double>> Coordinates { get; set; } = new List<List<double>>();
        }
    }
}

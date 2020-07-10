//namespace Pwe.GeoJson
//{
//    public class GeoJsonWriter
//    {
//        public static string ToGeoJsonMultiLine(MapTile tile, bool addBoundingBox, bool addWays = true)
//            => ToGeoJsonMultiLine(new List<MapTile> { tile }, addBoundingBox, addWays);

//        public static string ToGeoJsonMultiLine(List<MapTile> tiles, bool addBoundingBox, bool addWays = true)
//        {
//            var result = new GeoJsonMultiLine();
//            if (addWays)
//            {
//                foreach (var tile in tiles)
//                {
//                    var bounds = GetTileBounds(tile.Id);
//                    foreach (var node in tile.Nodes)
//                    {
//                        bool isInsideBounds = IsInsideBounds(node.Lon, node.Lat, bounds);
//                        foreach (var connectedId in node.Conn)
//                        {
//                            if (isInsideBounds && !node.Inside)
//                                throw new ArgumentException("Disagreement: inside or not?");

//                            var connectedNode = tile.GetNode(connectedId);
//                            var segment = new List<List<double>>();
//                            segment.Add(new List<double> { node.Lon, node.Lat });
//                            segment.Add(new List<double> { connectedNode.Lon, connectedNode.Lat });
//                            result.coordinates.Add(segment);
//                        }
//                    }
//                }
//            }

//            if (addBoundingBox)
//            {
//                foreach (var tile in tiles)
//                {
//                    var bbox = new List<List<double>>();
//                    var bounds = GetTileBounds(tile.Id);
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
//                    bbox.Add(new List<double> { bounds.lon1, bounds.lat0 });
//                    bbox.Add(new List<double> { bounds.lon1, bounds.lat1 });
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat1 });
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
//                    result.coordinates.Add(bbox);
//                }
//            }

//            return JsonConvert.SerializeObject(result);
//        }

//        public static string ToGeoJsonMultiPoint(List<MapTile> tiles, bool addBoundingBox)
//        {
//            var result = new GeoJsonGeometryCollection();
//            var points = new GeoJsonMultiPoint();
//            result.geometries.Add(points);

//            foreach (var tile in tiles)
//            {
//                var bounds = GetTileBounds(tile.Id);
//                foreach (var node in tile.Nodes.Where(n => !n.Inside))
//                {
//                    bool isInside = IsInsideBounds(node.Lon, node.Lat, bounds);
//                    long tileId = MapTileUtil.GetTileId(node.Lon, node.Lat);
//                    if (tile.Id == tileId)
//                        points.coordinates.Add(new List<double> { node.Lon, node.Lat });
//                }
//            }

//            if (addBoundingBox)
//            {
//                var geoBounds = new GeoJsonMultiLine();
//                result.geometries.Add(geoBounds);
//                foreach (var tile in tiles)
//                {
//                    var bbox = new List<List<double>>();
//                    var bounds = GetTileBounds(tile.Id);
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
//                    bbox.Add(new List<double> { bounds.lon1, bounds.lat0 });
//                    bbox.Add(new List<double> { bounds.lon1, bounds.lat1 });
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat1 });
//                    bbox.Add(new List<double> { bounds.lon0, bounds.lat0 });
//                    geoBounds.coordinates.Add(bbox);
//                }
//            }

//            return JsonConvert.SerializeObject(result);
//        }

//        class GeoJsonGeometryCollection
//        {
//            public string type { get; } = "GeometryCollection";
//            public List<object> geometries = new List<object>();
//        }

//        class GeoJsonMultiLine
//        {
//            public string type { get; } = "MultiLineString";
//            public List<List<List<double>>> coordinates { get; set; } = new List<List<List<double>>>();
//        }

//        class GeoJsonMultiPoint
//        {
//            public string type { get; } = "MultiPoint";
//            public List<List<double>> coordinates { get; set; } = new List<List<double>>();
//        }
//    }
//}

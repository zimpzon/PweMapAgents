using System;

namespace Pwe.Shared
{
    public static class TileMath
    {
        public static (double tileX, double tileY) WorldToTilePos(double lon, double lat, int zoom)
        {
            double tileX = ((lon + 180.0) / 360.0 * (1 << zoom));
            double tileY = ((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
                1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

            return (tileX, tileY);
        }

        public static (double lon, double lat) TileToWorldPos(double tileX, double tileY, int zoom)
        {
            double n = Math.PI - ((2.0 * Math.PI * tileY) / Math.Pow(2.0, zoom));
            double lon = ((tileX / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            double lat = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
            return (lon, lat);
        }

        public static long GetTileNeighborId(long tileId, int offsetX, int offsetY)
        {
            long tileX = (tileId & 0xffffffff) + offsetX;
            long tileY = (tileId >> 32) + offsetY;
            return tileX + (tileY << 32);
        }

        public static long GetTileId(double lon, double lat, int zoom)
        {
            var (tileX, tileY) = WorldToTilePos(lon, lat, zoom);
            long x = (long)tileX;
            long y = (long)tileY;
            return x + (y << 32);
        }

        public static bool IsInsideBounds(double lon, double lat, (double lon0, double lat0, double lon1, double lat1) bounds)
            => lon >= bounds.lon0 && lon < bounds.lon1 && lat < bounds.lat0 && lat >= bounds.lat1;

        public static (double lon0, double lat0, double lon1, double lat1) GetTileBounds(long tileId, int zoom)
        {
            long tileX = (tileId & 0xffffffff);
            long tileY = (tileId >> 32);
            var topLeft = TileToWorldPos(tileX, tileY, zoom);
            var bottomRight = TileToWorldPos(tileX + 1, tileY + 1, zoom);
            return (topLeft.lon, topLeft.lat, bottomRight.lon, bottomRight.lat);
        }
    }
}

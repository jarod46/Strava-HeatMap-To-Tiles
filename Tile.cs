using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StravaHeatMapToKMZ
{
    public class Tile
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public double north;
        public double south;
        public double east;
        public double west;
        public int zoom;
        public string tag = string.Empty;

        public Tile(Point point1, Point point2, int zoom)
        {
            this.zoom = zoom;
            x1 = point1.X;
            y1 = point1.Y;
            x2 = point2.X;
            y2 = point2.Y;
            var worldPos1 = TileToWorldPos(point1.X, point1.Y, zoom);
            var worldPos2 = TileToWorldPos(point2.X, point2.Y, zoom);
            west = worldPos1.X;
            north = worldPos1.Y;
            east = worldPos2.X;
            south = worldPos2.Y;
        }

        public Tile(double north, double south, double east, double west, int zoom)
        {
            this.zoom = zoom;
            this.north = north;
            this.south = south;
            this.east = east;
            this.west = west;
            var tile1 = WorldToTilePos(west, north, zoom);
            var tile2 = WorldToTilePos(east, south, zoom);
            x1 = (int)tile1.X;
            y1 = (int)tile1.Y;
            x2 = (int)tile2.X;
            y2 = (int)tile2.Y;
        }

        public static PointF WorldToTilePos(double lon, double lat, int zoom)
        {
            PointF p = new Point();
            p.X = (float)((lon + 180.0) / 360.0 * (1 << zoom));
            p.Y = (float)((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) +
                1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * (1 << zoom));

            return p;
        }

        public static PointF TileToWorldPos(double tile_x, double tile_y, int zoom)
        {
            PointF p = new Point();
            double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));

            p.X = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            p.Y = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            return p;
        }
    }
}

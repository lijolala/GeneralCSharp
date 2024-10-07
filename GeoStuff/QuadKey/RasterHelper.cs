using BitMiracle.LibTiff.Classic;
using NetVips;

using System;

public class RasterHelper
{
    // ModelTiepointTag (tag ID: 33922) and ModelPixelScaleTag (tag ID: 33550)
    const int ModelTiepointTag = 33922;
    const int ModelPixelScaleTag = 33550;

    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, short level) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        short level = (short)quadKey.Length;

        for (int i = level; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[level - i])
            {
                case '0':
                    break;
                case '1':
                    tileX |= mask;
                    break;
                case '2':
                    tileY |= mask;
                    break;
                case '3':
                    tileX |= mask;
                    tileY |= mask;
                    break;
                default:
                    throw new ArgumentException("Invalid QuadKey digit.");
            }
        }

        return (tileX, tileY, level);
    }

    // Convert Tile Coordinates to Bounding Box (Lat/Lon)
    public static (double north, double south, double east, double west) TileXYToBoundingBox(int tileX, int tileY,
        int level)
    {
        double n = Math.Pow(2.0, level);

        double lonMin = tileX / n * 360.0 - 180.0;
        double lonMax = (tileX + 1) / n * 360.0 - 180.0;

        double latMinRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
        double latMaxRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n)));

        double latMin = latMinRad * (180.0 / Math.PI);
        double latMax = latMaxRad * (180.0 / Math.PI);

        return (lonMin, latMin, lonMax, latMax);
    }

    public static (int pixelX, int pixelY) GeoToPixel(double lat, double lon, double[] tiePoint, double[] pixelScale)
    {
        double pixelX = (lon - tiePoint[0]) / pixelScale[0];
        double pixelY = (lat - tiePoint[1]) / -pixelScale[1]; // Note: y is flipped in many coordinate systems

        return ((int)Math.Round(pixelX), (int)Math.Round(pixelY));
    }

    public static (int column, int row) MapToPixel(double x, double y, double longitude, double latitude,
        double pixelWidth, double pixelHeight)
    {
        int column = (int)Math.Round((x - longitude - pixelWidth / 2) / pixelWidth);
        int row = (int)Math.Round((-y + latitude - pixelHeight / 2) / pixelHeight);
        return (column, row);
    }

    static double[] ByteArrayToDoubleArray(byte[] byteArray)
    {
        int doubleSize = sizeof(double);
        int count = byteArray.Length / doubleSize;
        double[] result = new double[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToDouble(byteArray, i * doubleSize);
        }

        return result;
    }

    public static double[] GetGeoTransform(Tiff image)
    {
        FieldValue[] pixelScaleValues = image.GetField((TiffTag)ModelPixelScaleTag);
        double[] pixelScales = { 1.0, 1.0 }; // Default pixel scales

        if (pixelScaleValues != null && pixelScaleValues.Length > 0)
        {
            byte[] byteArray = pixelScaleValues[1].GetBytes();
            pixelScales = ByteArrayToDoubleArray(byteArray);
        }

        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues == null || tiePointsValues.Length == 0)
            throw new Exception("ModelTiepointTag not found in the GeoTIFF.");

        byte[] tiePointsArray = tiePointsValues[1].GetBytes();
        double[] tiePoints = ByteArrayToDoubleArray(tiePointsArray);

        double[] geoTransform = new double[6];
        geoTransform[0] = tiePoints[3]; // Top-left X
        geoTransform[1] = pixelScales[0]; // Pixel width
        geoTransform[3] = tiePoints[4]; // Top-left Y
        geoTransform[5] = -pixelScales[1]; // Pixel height

        return geoTransform;
    }

    public static double[] GetGeoTransformFromPath(string tiffPath)
    {
        using (Tiff tif = Tiff.Open(tiffPath, "r"))
        {
            if (tif == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return null;
            }

            return GetGeoTransform(tif);
        }
    }

    public static double[] GetModelPixelScales(Tiff image)
    {
        FieldValue[] pixelScaleValues = image.GetField((TiffTag)ModelPixelScaleTag);
        double[] pixelScales = { 1.0, 1.0 }; // Default pixel scales

        if (pixelScaleValues != null && pixelScaleValues.Length > 0)
        {
            byte[] byteArray = pixelScaleValues[1].GetBytes();
            pixelScales = ByteArrayToDoubleArray(byteArray);
        }

        return pixelScales;
    }

    public static double[] GetModeTiePoints(Tiff image)
    {
        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues == null || tiePointsValues.Length == 0)
            return new double[] { 0, 0 };

        byte[] tiePointsArray = tiePointsValues[1].GetBytes();
        return ByteArrayToDoubleArray(tiePointsArray);
    }

    // Convert Pixel Coordinates to Latitude and Longitude
    public static (double lat, double lon) PixelXYToLatLong(int pixelX, int pixelY, int level)
    {
        double mapSize = 256 << level;
        double x = (pixelX / mapSize) - 0.5;
        double y = 0.5 - (pixelY / mapSize);

        double lat = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
        double lon = 360 * x;

        return (lat, lon);
    }

    static Tuple<double, double> ConvertGeoToPixel(double lat, double lon, double mapWidth, double mapHeight,
        double mapLonLeft, double mapLonDelta, double mapLatBottomDegree)
    {
        // Convert longitude to x coordinate
        double x = (lon - mapLonLeft) * (mapWidth / mapLonDelta);

        // Convert latitude to y coordinate
        lat = lat * Math.PI / 180;
        double worldMapWidth = ((mapWidth / mapLonDelta) * 360) / (2 * Math.PI);
        double mapOffsetY = (worldMapWidth / 2 *
                             Math.Log((1 + Math.Sin(mapLatBottomDegree)) / (1 - Math.Sin(mapLatBottomDegree))));
        double y = mapHeight - ((worldMapWidth / 2 * Math.Log((1 + Math.Sin(lat)) / (1 - Math.Sin(lat)))) - mapOffsetY);

        return Tuple.Create(x, y);
    }

    static void ConvertGeoToPixls1(double lon, double lat, double mapHeight, double mapWidth)
    {

        var PI = Math.PI;
        var x = (lon + 180) * (mapWidth / 360);
        // convert from degrees to radians
        var latRad = lat * PI / 180;
        // get y value
        var mercN = Math.Log(Math.Tan(PI / 4) + (latRad / 2));
        var y = (mapHeight / 2) - (mapWidth * mercN / (2 * PI));


    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    public static (double x, double y) LatLonToOffsets(double latitude, double longitude, double mapWidth,
        double mapHeight)
    {
        const double FE = 180; // false easting
        double radius = mapWidth / (2 * Math.PI);

        double latRad = DegreesToRadians(latitude);
        double lonRad = DegreesToRadians(longitude + FE);

        // X coordinate (longitude to x)
        double x = lonRad * radius;

        // Y coordinate (latitude to y)
        double yFromEquator = radius * Math.Log(Math.Tan(Math.PI / 4 + latRad / 2));
        double y = mapHeight / 2 - yFromEquator;

        return (x, y);
    }
    public static double[] TileXYToBoundingBox2(int tileX, int tileY, double zoom, int tileSize)
    {
        //Top left corner pixel coordinates
        var x1 = (double)(tileX * tileSize);
        var y1 = (double)(tileY * tileSize);

        //Bottom right corner pixel coordinates
        var x2 = (double)(x1 + tileSize);
        var y2 = (double)(y1 + tileSize);

        var nw = GlobalPixelToPosition(new double[] { x1, y1 }, zoom, tileSize);
        var se = GlobalPixelToPosition(new double[] { x2, y2 }, zoom, tileSize);


        return new double[] { nw[0], se[1], se[0], nw[1] };
    }
    /// <summary>
    /// Global Converts a Pixel coordinate into a geospatial coordinate at a specified zoom level. 
    /// Global Pixel coordinates are relative to the top left corner of the map (90, -180)
    /// </summary>
    /// <param name="pixel">Pixel coordinates in the format of [x, y].</param>  
    /// <param name="zoom">Zoom level</param>
    /// <param name="tileSize">The size of the tiles in the tile pyramid.</param>
    /// <returns>A position value in the format [longitude, latitude].</returns>
    public static double[] GlobalPixelToPosition(double[] pixel, double zoom, int tileSize)
    {
        var mapSize = MapSize(zoom, tileSize);

        var x = (Clip(pixel[0], 0, mapSize - 1) / mapSize) - 0.5;
        var y = 0.5 - (Clip(pixel[1], 0, mapSize - 1) / mapSize);

        return new double[] {
            360 * x,    //Longitude
            90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI  //Latitude
        };
    }
    /// <summary>
    /// Clips a number to the specified minimum and maximum values.
    /// </summary>
    /// <param name="n">The number to clip.</param>
    /// <param name="minValue">Minimum allowable value.</param>
    /// <param name="maxValue">Maximum allowable value.</param>
    /// <returns>The clipped value.</returns>
    private static double Clip(double n, double minValue, double maxValue)
    {
        return Math.Min(Math.Max(n, minValue), maxValue);
    }
    /// <summary>
    /// Calculates width and height of the map in pixels at a specific zoom level from -180 degrees to 180 degrees.
    /// </summary>
    /// <param name="zoom">Zoom Level to calculate width at</param>
    /// <param name="tileSize">The size of the tiles in the tile pyramid.</param>
    /// <returns>Width and height of the map in pixels</returns>
    public static double MapSize(double zoom, int tileSize)
    {
        return Math.Ceiling(tileSize * Math.Pow(2, zoom));
    }

    public class BoundingBox
    {
        public double North { get; set; }
        public double South { get; set; }
        public double East { get; set; }
        public double West { get; set; }
    }

    public static class TileHelper
    {
       public static (double north, double south, double east, double west) TileToBoundingBox(int x, int y, int zoom)
        {
            BoundingBox bb = new BoundingBox
            {
                North = TileToLat(y, zoom),
                South = TileToLat(y + 1, zoom),
                West = TileToLon(x, zoom),
                East = TileToLon(x + 1, zoom)
            };
            return (bb.North, bb.South, bb.East, bb.West);
        }

        public static double TileToLon(int x, int z)
        {
            return x / Math.Pow(2.0, z) * 360.0 - 180;
        }

        public static double TileToLat(int y, int z)
        {
            double n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2.0, z);
            return RadiansToDegrees(Math.Atan(Math.Sinh(n)));
        }
        private static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }
    }


    public static (int pixelX, int pixelY) LatLonToPixel2(double lat, double lon, double zoom, int tileSize)
    {
        // World map dimensions at the given zoom level
        double worldSize = tileSize * Math.Pow(2, zoom);

        // Convert longitude to X pixel coordinate
        double x = (lon + 180.0) / 360.0 * worldSize;

        // Convert latitude to Y pixel coordinate (Mercator projection)
        double sinLat = Math.Sin(lat * Math.PI / 180.0);
        double y = (0.5 - (Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI))) * worldSize;

        // Return pixel coordinates as an array
        return ((int)x, (int)y);
    }

    public static (int x, int y) ConvertToPixel(double longitude, double latitude, int mapWidth, int mapHeight)
    {
        // Convert longitude to x-coordinate
        double x = ((longitude + 180) / 360) * mapWidth;

        // Convert latitude to y-coordinate
        double y = ((90 - latitude) / 180) * mapHeight;

        return ((int)x, (int)y);
    }

    //////////////////////////////
    /// // Converts latitude to pixel Y coordinate
    static int LatToPixelY(double lat, int zoomLevel)
    {
        double sinLatitude = Math.Sin(lat * Math.PI / 180);
        int pixelY = (int)((0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI)) * (256 << zoomLevel));
        return pixelY;
    }

    // Converts longitude to pixel X coordinate
    static int LonToPixelX(double lon, int zoomLevel)
    {
        int pixelX = (int)((lon + 180.0) / 360.0 * (256 << zoomLevel));
        return pixelX;
    }


}


//not working
public class GeoToPixelConverter
{
    public double West { get; set; }
    public double East { get; set; }
    public double North { get; set; }
    public double South { get; set; }

    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public GeoToPixelConverter(double north, double south, double east, double west, int imageWidth, int imageHeight)
    {
        North = north;
        South = south;
        East = east;
        West = west;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
    }

    public (int pixelX, int pixelY) LatLonToPixel(double latitude, double longitude)
    {
        // Convert longitude to X pixel
        double pixelX = (longitude - West) / (East - West) * ImageWidth;

        // Convert latitude to Y pixel using Web Mercator projection
        double latRad = Math.PI * latitude / 180.0;
        double northRad = Math.PI * North / 180.0;

        double mercatorLat = Math.Log(Math.Tan(Math.PI / 4.0 + latRad / 2.0));
        double mercatorNorth = Math.Log(Math.Tan(Math.PI / 4.0 + northRad / 2.0));

        double pixelY = (1 - (mercatorLat / mercatorNorth)) * ImageHeight;

        return ((int)pixelX, (int)pixelY);
    }

}

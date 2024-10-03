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
    public static (double north, double south, double east, double west) TileXYToBoundingBox(int tileX, int tileY, int level)
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
    public static(int column, int row) MapToPixel(double x, double y, double longitude, double latitude, 
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
    public static double[] GetGeoTransform(Tiff image) {
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

    public static double[] GetModeTiePoints(Tiff image) {
        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues == null || tiePointsValues.Length == 0)
            return new double[] {0, 0};

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

    static Tuple<double, double> ConvertGeoToPixel(double lat, double lon, double mapWidth, double mapHeight, double mapLonLeft, double mapLonDelta, double mapLatBottomDegree)
    {
        // Convert longitude to x coordinate
        double x = (lon - mapLonLeft) * (mapWidth / mapLonDelta);

        // Convert latitude to y coordinate
        lat = lat * Math.PI / 180;
        double worldMapWidth = ((mapWidth / mapLonDelta) * 360) / (2 * Math.PI);
        double mapOffsetY = (worldMapWidth / 2 * Math.Log((1 + Math.Sin(mapLatBottomDegree)) / (1 - Math.Sin(mapLatBottomDegree))));
        double y = mapHeight - ((worldMapWidth / 2 * Math.Log((1 + Math.Sin(lat)) / (1 - Math.Sin(lat)))) - mapOffsetY);

        return Tuple.Create(x, y);
    }

    static void ConvertGeoToPixls1(double lon, double lat, double mapHeight, double mapWidth) {

        var PI = Math.PI;
        var x = (lon + 180) * (mapWidth / 360);
        // convert from degrees to radians
        var latRad = lat * PI / 180;
        // get y value
        var mercN = Math.Log(Math.Tan(PI / 4) + (latRad / 2));
        var y = (mapHeight / 2) - (mapWidth * mercN / (2 * PI));


    }
}
using System;

using BitMiracle.LibTiff.Classic;

class QuadKeyToGeoTIFF
{
    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, int level) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int level = quadKey.Length;

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

    // Read GeoTIFF Metadata using libtiff
    public static void ReadGeoTiff(string filePath, int zoomLevel)
    {
        using (Tiff tiff = Tiff.Open(filePath, "r"))
        {
            if (tiff == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }
            if (tiff.NumberOfDirectories() > zoomLevel)
            {
                tiff.SetDirectory((short)zoomLevel);
            }
            else
            {
                tiff.SetDirectory((short)tiff.NumberOfDirectories());
            }
            const int ModelTiepointTag = 33922;
            const int ModelPixelScaleTag = 33550;
            // Fetch the model pixel scale tag (scaling)
            FieldValue[] pixelScaleValues = tiff.GetField((TiffTag)ModelPixelScaleTag);
            double[] modelPixelScaleTag = { 1.0, 1.0 }; // Default pixel scales
            byte[] byteArray = pixelScaleValues[1].GetBytes();
            modelPixelScaleTag = ByteArrayToDoubleArray(byteArray);
            if (modelPixelScaleTag != null)
            {
                double scaleX = modelPixelScaleTag[0];
                double scaleY = modelPixelScaleTag[1];
                Console.WriteLine($"ScaleX: {scaleX}, ScaleY: {scaleY}");
            }

            // Fetch the model tiepoint tag (mapping between image space and geographical space)
            FieldValue[] modelTiepointTag = tiff.GetField((TiffTag)ModelTiepointTag);
            if (modelTiepointTag != null)
            {
                byte[] tiePointsArray = modelTiepointTag[1].GetBytes();
                double[] tiePoints = ByteArrayToDoubleArray(tiePointsArray);
                double imageX = tiePoints[0];
                double imageY = tiePoints[1];
                double imageZ = tiePoints[2];
                double geoX = tiePoints[3];
                double geoY = tiePoints[4];
                double geoZ = tiePoints[5];

                Console.WriteLine($"Image (X, Y, Z): ({imageX}, {imageY}, {imageZ})");
                Console.WriteLine($"Geo (X, Y, Z): ({geoX}, {geoY}, {geoZ})");
            }
        }
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

    public static void Main(string[] args)
    {
        // Example quadkey
        string quadKey = "1202102332";
        var (tileX, tileY, level) = QuadKeyToTileXY(quadKey);
        var (left, bottom, right, top) = TileXYToBoundingBox(tileX, tileY, level);

        Console.WriteLine($"QuadKey: {quadKey}");
        Console.WriteLine($"TileX: {tileX}, TileY: {tileY}, Level: {level}");
        //Console.WriteLine($"Bounding Box: North={north}, South={bottom}, East={east}, West={west}");

        // Read GeoTIFF file
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        ReadGeoTiff(filePath, level);
    }
}

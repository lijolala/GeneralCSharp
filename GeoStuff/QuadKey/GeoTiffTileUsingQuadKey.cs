using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string filePath =
                   @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; // Replace with your GeoTIFF file path
        string quadKey = "122002"; // Replace with your QuadKey
        string outputTilePath =
            @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";
        ExtractAndSaveTile(filePath, quadKey, outputTilePath);
    }

    public static void ExtractAndSaveTile(string geoTiffPath, string quadKey, string outputTilePath)
    {
        var (zoomLevel, tileX, tileY) = QuadKeyToTile(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileToBoundingBox(tileX, tileY, zoomLevel);

        using (Tiff image = Tiff.Open(geoTiffPath, "r"))
        {
            if (image.NumberOfDirectories() > zoomLevel)
            {
                image.SetDirectory((short)zoomLevel);
            }
            else
            {
                image.SetDirectory((short)image.NumberOfDirectories());
                throw new Exception($"Zoom level {zoomLevel} not found in the GeoTIFF.");
            }
            double[] geoTransform = GetGeoTransform(image);
            int minXPixel, minYPixel, maxXPixel, maxYPixel;

            // Convert the bounding box coordinates to pixel coordinates
            GeoToPixel(minLon, minLat, geoTransform, out minXPixel, out minYPixel);
            GeoToPixel(maxLon, maxLat, geoTransform, out maxXPixel, out maxYPixel);

            // Ensure the area extracted is 256x256 pixels
            int tileWidth = 256;
            int tileHeight = 256;

            // Allocate buffer for the tile
            byte[] tileBuffer = new byte[tileWidth * tileHeight * 4]; // Assuming 4 bytes per pixel (RGBA)

            // Adjust min and max coordinates to fit the 256x256 size
            minXPixel = Math.Max(minXPixel, 0);
            minYPixel = Math.Max(minYPixel, 0);
            maxXPixel = Math.Min(maxXPixel, image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt());
            maxYPixel = Math.Min(maxYPixel, image.GetField(TiffTag.IMAGELENGTH)[0].ToInt());

            // Read the tile data from the GeoTIFF
            ReadTile(image, minXPixel, minYPixel, tileWidth, tileHeight, tileBuffer);

            // Save the tile as PNG
            SaveTileAsPng(tileBuffer, tileWidth, tileHeight, outputTilePath);
        }
    }

    private static void ReadTile(Tiff image, int minX, int minY, int width, int height, byte[] buffer)
    {
        int bytesPerPixel = 4; // RGBA format
        for (int y = 0; y < height; y++)
        {
            image.ReadScanline(buffer, y * width * bytesPerPixel);
        }
    }

    public static double[] GetGeoTransform(Tiff image)
    {
        // ModelTiepointTag (tag ID: 33922) and ModelPixelScaleTag (tag ID: 33550)
        const int ModelTiepointTag = 33922;
        const int ModelPixelScaleTag = 33550;

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

    public static (int zoomLevel, int tileX, int tileY) QuadKeyToTile(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int zoomLevel = quadKey.Length;

        for (int i = zoomLevel; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[zoomLevel - i])
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

        return (zoomLevel, tileX, tileY);
    }

    public static (double minLon, double minLat, double maxLon, double maxLat) TileToBoundingBox(int tileX, int tileY, int zoomLevel)
    {
        double n = Math.Pow(2.0, zoomLevel);

        double lon1 = tileX / n * 360.0 - 180.0;
        double lon2 = (tileX + 1) / n * 360.0 - 180.0;
        double lat1 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n))) * (180.0 / Math.PI);
        double lat2 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n))) * (180.0 / Math.PI);

        return (lon1, lat2, lon2, lat1);
    }

    public static void GeoToPixel(double lon, double lat, double[] geoTransform, out int pixelX, out int pixelY)
    {
        pixelX = (int)((lon - geoTransform[0]) / geoTransform[1]);
        pixelY = (int)((lat - geoTransform[3]) / geoTransform[5]);
    }

    public static void SaveTileAsPng(byte[] buffer, int tileWidth, int tileHeight, string outputPath)
    {
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            bitmap.Save(outputPath, ImageFormat.Png);
        }
    }
}

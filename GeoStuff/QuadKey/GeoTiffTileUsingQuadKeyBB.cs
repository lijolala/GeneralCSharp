using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; // Replace with your GeoTIFF file path
        string quadkey = "120210233222"; // Replace with your quadkey
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1"; // Replace with your output folder path

        // Decode quadkey to get zoom level and tile indices
        var result = DecodeQuadkey(quadkey);
        int zoomLevel = result.Item1;
        int tileX = result.Item2;
        int tileY = result.Item3;

        // Get the bounding box for the quadkey tile in Web Mercator coordinates
        (double minX, double minY, double maxX, double maxY) = QuadkeyToBounds(tileX, tileY, zoomLevel);

        // Open the GeoTIFF file
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image.NumberOfDirectories() > zoomLevel)
            {
                image.SetDirectory((short)zoomLevel);
            }
            else
            {
                image.SetDirectory((short)image.NumberOfDirectories())
                throw new Exception($"Zoom level {zoomLevel} not found in the GeoTIFF.");
            }
            // Get image information from the GeoTIFF directory
            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int tileSize = image.TileSize();
            byte[] tileBuffer = new byte[tileSize];

            // Get the GeoTIFF's bounding box (assuming it's in Web Mercator)
            double[] tiePoints = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG)[0].ToDoubleArray();
            double[] pixelScale = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG)[0].ToDoubleArray();

            // Compute the bounding box of the GeoTIFF (in Web Mercator coordinates)
            double tiffMinX = tiePoints[3];
            double tiffMaxY = tiePoints[4];
            double tiffMaxX = tiffMinX + (imageWidth * pixelScale[0]);
            double tiffMinY = tiffMaxY - (imageHeight * pixelScale[1]);

            // Convert the Web Mercator quadkey bounds to pixel coordinates in the GeoTIFF
            int pixelMinX = (int)((minX - tiffMinX) / pixelScale[0]);
            int pixelMaxX = (int)((maxX - tiffMinX) / pixelScale[0]);
            int pixelMinY = (int)((tiffMaxY - maxY) / pixelScale[1]);
            int pixelMaxY = (int)((tiffMaxY - minY) / pixelScale[1]);

            // Extract and save the tile as PNG
            ExtractAndSaveTile(image, pixelMinX, pixelMinY, pixelMaxX, pixelMaxY, outputFolder, tileX, tileY);
        }
    }

    static void ExtractAndSaveTile(Tiff image, int pixelMinX, int pixelMinY, int pixelMaxX, int pixelMaxY, string outputFolder, int tileX, int tileY)
    {
        int width = pixelMaxX - pixelMinX;
        int height = pixelMaxY - pixelMinY;
        byte[] buffer = new byte[width * height * 4]; // Assuming 32-bit RGBA

        // Copy the tile's pixel data into the buffer (this assumes a single strip - adjust if multi-strip)
        for (int y = pixelMinY; y < pixelMaxY; y++)
        {
            int readOffset = y * width * 4;
            image.ReadEncodedStrip(y, buffer, readOffset, buffer.Length - readOffset);
        }

        // Save the tile as a PNG
        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            // Construct file path and save as PNG
            string fileName = Path.Combine(outputFolder, $"tile_{tileX}_{tileY}.png");
            bitmap.Save(fileName, ImageFormat.Png);
        }
    }

    static (double minX, double minY, double maxX, double maxY) QuadkeyToBounds(int tileX, int tileY, int zoomLevel)
    {
        double tileCount = Math.Pow(2, zoomLevel);
        double minX = tileX / tileCount * 360.0 - 180.0;
        double minY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / tileCount))) * 180.0 / Math.PI;
        double maxX = (tileX + 1) / tileCount * 360.0 - 180.0;
        double maxY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / tileCount))) * 180.0 / Math.PI;

        // Convert to Web Mercator meters
        return (LonToMeters(minX), LatToMeters(minY), LonToMeters(maxX), LatToMeters(maxY));
    }

    static double LonToMeters(double lon)
    {
        return lon * 20037508.34 / 180.0;
    }

    static double LatToMeters(double lat)
    {
        double latRad = lat * Math.PI / 180.0;
        return Math.Log(Math.Tan((Math.PI / 4) + (latRad / 2))) * 20037508.34 / Math.PI;
    }

    static Tuple<int, int, int> DecodeQuadkey(string quadkey)
    {
        int zoomLevel = quadkey.Length;
        int tileX = 0, tileY = 0;

        for (int i = 0; i < zoomLevel; i++)
        {
            int mask = 1 << (zoomLevel - 1 - i);
            switch (quadkey[i])
            {
                case '1': tileX |= mask; break;
                case '2': tileY |= mask; break;
                case '3': tileX |= mask; tileY |= mask; break;
            }
        }

        return Tuple.Create(zoomLevel, tileX, tileY);
    }
}

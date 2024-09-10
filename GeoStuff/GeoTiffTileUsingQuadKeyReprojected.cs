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
        string quadKey = "1202312231"; // Replace with your quadkey
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1"; // Replace with your output folder path

        var (zoomLevel, tileX, tileY) = QuadKeyToTile(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileToBoundingBox(tileX, tileY, zoomLevel);

       
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            int tileSize = image.TileSize();
            byte[] buffer = new byte[tileSize];

            // Get tile width and height
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Read the tile into the buffer
            image.ReadTile(buffer, 0, tileX * tileWidth, tileY * tileHeight, 0, 0);
            // Save tileBuffer to a file (e.g., as PNG)
            SaveTileAsPng(buffer, outputFolder, tileWidth, tileHeight);
            ////// Retrieve the elevation for the current tile
            ////double elevation = GetElevationForTile(image, col, row, zoomLevel);
            ////SaveTileAsJpeg(buffer, tileWidth, tileHeight, col, row, elevation, outputFolder);
        }
    }
    public static (double minLon, double minLat, double maxLon, double maxLat) TileToBoundingBox(int tileX, int tileY, int zoomLevel)
    {
        double n = Math.Pow(2.0, zoomLevel);

        // Convert tile x and y to geographic coordinates in Web Mercator projection (EPSG:3857)
        double lon1 = tileX / n * 360.0 - 180.0;
        double lon2 = (tileX + 1) / n * 360.0 - 180.0;
        double lat1 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n))) * (180.0 / Math.PI);
        double lat2 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n))) * (180.0 / Math.PI);

        return (lon1, lat2, lon2, lat1);
    }
    public static void SaveTileAsPng(byte[] buffer, string outputPath, int tileWidth, int tileHeight)
    {
        // Example of saving a tile buffer as a PNG
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            // Lock the bitmap bits and copy the buffer into the bitmap
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);
            // Construct the file name including elevation
            string fileName = Path.Combine(outputPath, $"tile_1.png");
            // Save the bitmap as a PNG file
            bitmap.Save(fileName, ImageFormat.Png);
        }
    }

    static void SaveTileAsJpeg(byte[] buffer, int tileWidth, int tileHeight, int col, int row, double elevation, string outputFolder)
    {
        // Assuming 32-bit RGBA data in the buffer, create a Bitmap
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Copy the buffer data into the bitmap's pixel buffer
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);

            bitmap.UnlockBits(bmpData);

            // Construct the file name including elevation
            string fileName = Path.Combine(outputFolder, $"tile_{row}_{col}_elev_{elevation:F2}.png");

            // Save the bitmap as a JPEG file
            bitmap.Save(fileName, ImageFormat.Png);
        }
    }

    static double GetElevationForTile(Tiff image, int col, int row, int zoomLevel)
    {
        // For demonstration, assuming elevation is stored in a specific band or as a tag.
        // Adjust the logic depending on where elevation data is stored.

        // Example: If elevation is in the 3rd band (Z-coordinate)
        byte[] elevationBuffer = new byte[image.TileSize()];
        image.ReadTile(elevationBuffer, 0, col, row, 2, 0); // Assuming band 2 (zero-indexed) for elevation
        // Convert buffer to a meaningful elevation value. This might require specific conversion logic.
        // For now, let's assume the first pixel gives us the elevation.
        double elevation = BitConverter.ToSingle(elevationBuffer, 0);

        return elevation;
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

}

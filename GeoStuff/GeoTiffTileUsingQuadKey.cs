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
        string quadkey = "2111"; // Replace with your quadkey
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1"; // Replace with your output folder path

        var result = DecodeQuadkey(quadkey);
        int zoomLevel = result.Item1;
        int row = result.Item2;
        int col = result.Item3;
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            int tileSize = image.TileSize();
            byte[] buffer = new byte[tileSize];
            // Get tile width and height
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Read the tile into the buffer
            image.ReadTile(buffer, 0, col * tileWidth, row * tileHeight, 0, 0);
            // Retrieve the elevation for the current tile
            double elevation = GetElevationForTile(image, col, row, zoomLevel);
            SaveTileAsJpeg(buffer, tileWidth, tileHeight, col, row, elevation, outputFolder);
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
    static Tuple<int, int, int> DecodeQuadkey(string quadkey)
    {
        if (string.IsNullOrEmpty(quadkey))
        {
            throw new ArgumentException("Quadkey cannot be null or empty.");
        }

        int zoomLevel = quadkey.Length;
        int tileX = 0;
        int tileY = 0;

        for (int i = zoomLevel - 1; i >= 0; i--)
        {
            char digit = quadkey[i];
            int mask = 1 << i;
            switch (digit)
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
                    throw new ArgumentException($"Invalid quadkey digit: {digit}");
            }
        }

        return Tuple.Create(zoomLevel, tileX, tileY);
    }

}

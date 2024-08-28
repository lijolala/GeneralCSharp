using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class TiffTileReader
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFullElevation";

        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            int zoomLevel = 1;

            // Loop through all directories (each representing a zoom level)
            do
            {
                Console.WriteLine($"\nProcessing Zoom Level {zoomLevel}:");

                // Create a directory for the current zoom level
                string zoomLevelFolder = Path.Combine(outputFolder, $"ZoomLevel_{zoomLevel}");
                Directory.CreateDirectory(zoomLevelFolder);

                // Check if the image is tiled
                bool isTiled = image.IsTiled();
                if (!isTiled)
                {
                    Console.WriteLine("The TIFF image is not tiled.");
                    continue;
                }

                // Get tile width and height
                int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

                // Calculate the number of tiles horizontally and vertically
                int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                int tilesAcross = (imageWidth + tileWidth - 1) / tileWidth;
                int tilesDown = (imageHeight + tileHeight - 1) / tileHeight;

                // Allocate a buffer for one tile
                int tileSize = image.TileSize();
                byte[] buffer = new byte[tileSize];

                // Iterate over all tiles
                for (int row = 0; row < tilesDown; row++)
                {
                    for (int col = 0; col < tilesAcross; col++)
                    {
                        int tileIndex = image.ComputeTile(col * tileWidth, row * tileHeight, 0, 0);

                        // Read the tile into the buffer
                        image.ReadTile(buffer, 0, col * tileWidth, row * tileHeight, 0, 0);

                        // Retrieve the elevation for the current tile
                        double elevation = GetElevationForTile(image, col, row);

                        // Save the tile as a JPEG image in the zoom level folder
                        SaveTileAsJpeg(buffer, tileWidth, tileHeight, col, row, elevation, zoomLevelFolder);
                    }
                }

                zoomLevel++;
            } while (image.ReadDirectory());

            Console.WriteLine("\nFinished processing all zoom levels.");
        }
    }

    static double GetElevationForTile(Tiff image, int col, int row)
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

    static void SaveTileAsJpeg(byte[] buffer, int tileWidth, int tileHeight, int col, int row, double elevation,
        string outputFolder)
    {
        // Assuming 32-bit RGBA data in the buffer, create a Bitmap
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight), ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            // Copy the buffer data into the bitmap's pixel buffer
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);

            bitmap.UnlockBits(bmpData);

            // Construct the file name including elevation
            string fileName = Path.Combine(outputFolder, $"tile_{row}_{col}_elev_{elevation:F2}.jpeg");

            // Save the bitmap as a JPEG file
            bitmap.Save(fileName, ImageFormat.Jpeg);
        }
    }

    static void ProcessTile(byte[] buffer, int tileWidth, int tileHeight)
    {
        // Implement your custom processing logic here
        Console.WriteLine($"Processing tile of size {tileWidth}x{tileHeight}");
    }
}
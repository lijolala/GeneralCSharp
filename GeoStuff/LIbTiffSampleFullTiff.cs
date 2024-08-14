
using System;
using System.Diagnostics;
using System.Drawing;
using BitMiracle.LibTiff.Classic;

using BitMiracle.LibTiff.Classic;
using System;
using System.Drawing.Imaging;
using System.IO;

class TiffTileReader
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump";

        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            // Check if the image is tiled
            bool isTiled = image.IsTiled();
            if (!isTiled)
            {
                Console.WriteLine("The TIFF image is not tiled.");
                return;
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
                    //int tileIndex = image.ComputeTile(col * tileWidth, row * tileHeight, 0, 0);

                    //// Read the tile into the buffer
                    //image.ReadTile(buffer, 0, col * tileWidth, row * tileHeight, 0, 0);

                    //// Process the tile buffer as needed
                    //ProcessTile(buffer, tileWidth, tileHeight);

                    int tileIndex = image.ComputeTile(col * tileWidth, row * tileHeight, 0, 0);

                    // Read the tile into the buffer
                    image.ReadTile(buffer, 0, col * tileWidth, row * tileHeight, 0, 0);

                    // Convert buffer to a JPEG image and save
                    SaveTileAsJpeg(buffer, tileWidth, tileHeight, col, row, outputFolder);
                }
            }
        }
    }
    static void SaveTileAsJpeg(byte[] buffer, int tileWidth, int tileHeight, int col, int row, string outputFolder)
    {
        // Assuming 32-bit RGBA data in the buffer, create a Bitmap
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Copy the buffer data into the bitmap's pixel buffer
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);

            bitmap.UnlockBits(bmpData);

            // Construct the file name
            string fileName = Path.Combine(outputFolder, $"tile_{row}_{col}.jpeg");

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


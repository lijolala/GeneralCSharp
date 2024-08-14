
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

            // Read the GeoTIFF's width and height
            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            int tilesAcross = (imageWidth + tileWidth - 1) / tileWidth;
            int tilesDown = (imageHeight + tileHeight - 1) / tileHeight;

            // Assuming pixel size in X and Y directions
            FieldValue[] pixelScaleField = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            if (pixelScaleField != null)
            {
                double[] pixelScale = pixelScaleField[1].ToDoubleArray();
                double pixelWidth = pixelScale[0];
                double pixelHeight = pixelScale[1];

                Console.WriteLine($"Pixel Size (Width x Height): {pixelWidth} x {pixelHeight} units");
            }

            double pixelSizeX = 1.0; // You should read the actual pixel size from GeoTIFF tags
            double pixelSizeY = 1.0;

            double minX = -0.489; // Example map extent
            double minY = 51.28;
            double maxX = 0.236;
            double maxY = 51.686;

            // array([ 2.15209704, 48.73039383,  2.54099598, 48.98700028])
            FieldValue[] modelPixelScaleTag = image.GetField((TiffTag)33550);

            if (modelPixelScaleTag != null && modelPixelScaleTag.Length > 1)
            {
                // The second element (index 1) contains the array of doubles as a byte array
                byte[] scaleValuesBytes = modelPixelScaleTag[1].GetBytes();

                // Convert the byte array to a double array
                double[] scaleValues = new double[scaleValuesBytes.Length / sizeof(double)];
                Buffer.BlockCopy(scaleValuesBytes, 0, scaleValues, 0, scaleValuesBytes.Length);

                if (scaleValues.Length >= 2)
                {
                    pixelSizeX = scaleValues[0];
                    pixelSizeY = scaleValues[1];

                    Console.WriteLine($"Pixel Size in X direction: {pixelSizeX}");
                    Console.WriteLine($"Pixel Size in Y direction: {pixelSizeY}");
                }
                else
                {
                    Console.WriteLine("The ModelPixelScaleTag does not contain enough data.");
                }
            }

            // Calculate pixel coordinates from the map extent
            int minPixelX = (int)((minX - 0) / pixelSizeX);
            int maxPixelX = (int)((maxX - 0) / pixelSizeX);
            int minPixelY = (int)((minY - 0) / pixelSizeY);
            int maxPixelY = (int)((maxY - 0) / pixelSizeY);

            // Ensure the pixel coordinates are within the image bounds
            minPixelX = Math.Max(0, minPixelX);
            maxPixelX = Math.Min(imageWidth - 1, maxPixelX);
            minPixelY = Math.Max(0, minPixelY);
            maxPixelY = Math.Min(imageHeight - 1, maxPixelY);

            // Read and process the tile data
            for (int y = minPixelY; y <= maxPixelY; y++)
            {
                for (int x = minPixelX; x <= maxPixelX; x++)
                {
                    byte[] bboxBuffer = new byte[4 * (maxPixelX - minPixelX + 1)];
                    // image.ReadScanline(bboxBuffer, y);
                    SaveTileAsJpeg(bboxBuffer, tileWidth, tileHeight, x, y, outputFolder);

                    // Process the buffer, e.g., extract a tile
                    // Here, you would save or display the image tile
                }
            }
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


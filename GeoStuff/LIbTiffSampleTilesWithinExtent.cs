
using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class TiffTileReader
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpExtent";

        double minX = -0.489; // Example map extent
        double minY = 80.28;
        double maxX = 0.236;
        double maxY = 101.686;
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
            double[] pixelScale = pixelScaleField[1].ToDoubleArray();
            double pixelWidth = pixelScale[0];
            double pixelHeight = pixelScale[1];


            // Get tie points (georeferencing information)
            FieldValue[] tiePointsField = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            double[] tiePoints = tiePointsField[1].ToDoubleArray();
            double originX = tiePoints[3]; // Geographic X coordinate of the top-left pixel
            double originY = tiePoints[4]; // Geographic Y coordinate of the top-left pixel

            // Calculate the pixel coordinates of the bounding box
            int minXPixel = (int)((minX - originX) / pixelWidth);
            int maxXPixel = (int)((maxX - originX) / pixelWidth);
            int minYPixel = (int)((originY - maxY) / pixelHeight); // Y coordinates are flipped
            int maxYPixel = (int)((originY - minY) / pixelHeight);

            // Calculate tile indices within the bounding box
            int minTileX = minXPixel / tileWidth;
            int maxTileX = maxXPixel / tileWidth;
            int minTileY = minYPixel / tileHeight;
            int maxTileY = maxYPixel / tileHeight;

            for (int tileY = minTileY; tileY <= maxTileY; tileY++)
            {
                for (int tileX = minTileX; tileX <= maxTileX; tileX++)
                {
                    int tileIndex = Math.Abs(image.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0));
                    byte[] tileData = new byte[tileWidth * tileHeight * 3]; // Assuming 24-bit RGB

                    if (image.ReadEncodedTile(tileIndex, tileData, 0, tileData.Length) > 0)
                    {
                        Console.WriteLine($"Tile ({tileX}, {tileY}) extracted.");
                        // Process or save the tileData as needed
                        SaveTileAsJpeg(tileData, tileWidth, tileHeight, tileY, tileX, outputFolder);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to read tile ({tileX}, {tileY}).");
                    }
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


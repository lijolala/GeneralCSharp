using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class Program
{
    static void Main(string[] args)
    {
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpSingleImage";

        using (Tiff image = Tiff.Open(tiffFilePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int photoMetric = image.GetField(TiffTag.PHOTOMETRIC)[0].ToInt();

            //if (samplesPerPixel != 3 || bitsPerSample != 8 || photoMetric != (int)Photometric.RGB)
            //{
            //    Console.WriteLine("This example assumes an 8-bit per channel, 3-sample (RGB) image.");
            //    return;
            //}

            // Define tile size (e.g., 1024x1024 pixels)
            int tileWidth = 512;
            int tileHeight = 512;

            // Buffer for reading scanlines
            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            // Iterate over tiles
            for (int tileY = 0; tileY < imageHeight; tileY += tileHeight)
            {
                int currentTileHeight = Math.Min(tileHeight, imageHeight - tileY);

                for (int tileX = 0; tileX < imageWidth; tileX += tileWidth)
                {
                    int currentTileWidth = Math.Min(tileWidth, imageWidth - tileX);

                    // Create a Bitmap for the current tile
                    using (Bitmap bitmap = new Bitmap(currentTileWidth, currentTileHeight, PixelFormat.Format24bppRgb))
                    {
                        // Lock the bitmap's bits for faster access
                        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, currentTileWidth, currentTileHeight),
                                                                ImageLockMode.WriteOnly, bitmap.PixelFormat);

                        IntPtr ptr = bitmapData.Scan0;
                        int stride = bitmapData.Stride;

                        for (int y = 0; y < currentTileHeight; y++)
                        {
                            // Read the corresponding scanline
                            image.ReadScanline(buffer, tileY + y);

                            for (int x = 0; x < currentTileWidth; x++)
                            {
                                int bufferIndex = (tileX + x) * samplesPerPixel;
                                int bitmapIndex = y * stride + x * 3; // 3 bytes per pixel (RGB)

                                Marshal.WriteByte(ptr, bitmapIndex + 2, buffer[bufferIndex]);     // Red
                                Marshal.WriteByte(ptr, bitmapIndex + 1, buffer[bufferIndex + 1]); // Green
                                Marshal.WriteByte(ptr, bitmapIndex, buffer[bufferIndex + 2]);     // Blue
                            }
                        }

                        // Unlock the bits
                        bitmap.UnlockBits(bitmapData);

                        // Save the tile as an image file
                        string tilePath = $"{outputFolder}/tile_{tileX}_{tileY}.png";
                        bitmap.Save(tilePath, ImageFormat.Png);
                    }

                    Console.WriteLine($"Saved tile {tileX}_{tileY}.");
                }
            }

            Console.WriteLine("All tiles saved successfully.");
        }
    }
}

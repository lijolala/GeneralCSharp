using System;
using System.Drawing;
using System.Drawing.Imaging;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class TiffTo24BitBitmap
    {
        public static void Main()
        {
            // Define the bounding box
            int x0 = 100; // Top-left x-coordinate of the bounding box
            int y0 = 100; // Top-left y-coordinate of the bounding box
            int boxWidth = 256; // Width of the bounding box
            int boxHeight = 256; // Height of the bounding box

            using (Tiff tif = Tiff.Open(@"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif", "r"))
            {
                // Find the width and height of the image
                FieldValue[] value = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = tif.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                // Adjust the bounding box to ensure it's within image bounds
                if (x0 + boxWidth > width) boxWidth = width - x0;
                if (y0 + boxHeight > height) boxHeight = height - y0;

                // Check if the image is tiled
                FieldValue[] tileWidthValue = tif.GetField(TiffTag.TILEWIDTH);
                FieldValue[] tileLengthValue = tif.GetField(TiffTag.TILELENGTH);

                if (tileWidthValue != null && tileLengthValue != null)
                {
                    int tileWidth = tileWidthValue[0].ToInt();
                    int tileLength = tileLengthValue[0].ToInt();

                    // Calculate the range of tiles that intersect with the bounding box
                    int tileXStart = x0 / tileWidth;
                    int tileXEnd = (x0 + boxWidth - 1) / tileWidth;
                    int tileYStart = y0 / tileLength;
                    int tileYEnd = (y0 + boxHeight - 1) / tileLength;

                    // Create a new bitmap to hold the cropped area
                    using (Bitmap bmp = new Bitmap(boxWidth, boxHeight, PixelFormat.Format24bppRgb))
                    {
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        BitmapData bmpdata = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                        byte[] bits = new byte[bmpdata.Stride * bmpdata.Height];

                        byte[] tileBuffer = new byte[tif.TileSize()];
                        int bitsOffset = 0;

                        // Process only the tiles within the bounding box range
                        for (int tileY = tileYStart; tileY <= tileYEnd; tileY++)
                        {
                            for (int tileX = tileXStart; tileX <= tileXEnd; tileX++)
                            {
                                // Tile top-left coordinates in image space
                                int tileXStartInImage = tileX * tileWidth;
                                int tileYStartInImage = tileY * tileLength;

                                // Read the tile into memory
                                int tileIndex = tif.ComputeTile(tileXStartInImage, tileYStartInImage, 0, 0);
                                tif.ReadEncodedTile(tileIndex, tileBuffer, 0, tileBuffer.Length);

                                // Calculate bounds of the intersection of the tile and the bounding box
                                int tileX0 = Math.Max(x0, tileXStartInImage);
                                int tileY0 = Math.Max(y0, tileYStartInImage);
                                int tileX1 = Math.Min(x0 + boxWidth, tileXStartInImage + tileWidth);
                                int tileY1 = Math.Min(y0 + boxHeight, tileYStartInImage + tileLength);

                                // Copy the relevant pixels from the tile to the bitmap
                                for (int y = tileY0; y < tileY1; y++)
                                {
                                    int tileYOffset = (y - tileYStartInImage) * tileWidth * 4; // 4 bytes per pixel (RGBA)

                                    for (int x = tileX0; x < tileX1; x++)
                                    {
                                        int rasterOffset = (x - tileXStartInImage) * 4;

                                        // Get the RGBA values from the tile buffer
                                        byte r = tileBuffer[tileYOffset + rasterOffset + 0];
                                        byte g = tileBuffer[tileYOffset + rasterOffset + 1];
                                        byte b = tileBuffer[tileYOffset + rasterOffset + 2];

                                        // Calculate where this pixel should go in the final cropped bitmap
                                        int bitsIndex = ((y - y0) * bmpdata.Stride) + (x - x0) * 3;
                                        bits[bitsIndex + 0] = r;
                                        bits[bitsIndex + 1] = g;
                                        bits[bitsIndex + 2] = b;
                                    }
                                }
                            }
                        }

                        // Copy the bits into the bitmap and unlock it
                        System.Runtime.InteropServices.Marshal.Copy(bits, 0, bmpdata.Scan0, bits.Length);
                        bmp.UnlockBits(bmpdata);

                        // Save the cropped image
                        bmp.Save("TiffTo24BitBitmap_Cropped.bmp");
                        System.Diagnostics.Process.Start("TiffTo24BitBitmap_Cropped.bmp");
                    }
                }
                else
                {
                    Console.WriteLine("The image is not tiled.");
                }
            }
        }
    }
}

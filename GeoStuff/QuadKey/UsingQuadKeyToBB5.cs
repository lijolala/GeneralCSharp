using System;
using System.Drawing;
using System.Drawing.Imaging;

using BitMiracle.LibTiff.Classic;

namespace BitMiracle.LibTiff.Samples
{
    public static class GeoTiffExtractor
    {
        public static void Main()
        {
            string quadKey = "1202102332";
            string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
            string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";
            var (QTileX, QTileY, level) = QuadKeyToTileXY(quadKey);
            var (minLon, minLat, maxLon, maxLat) = TileXYToBoundingBox(QTileX, QTileY, level);

            // Open the GeoTIFF
            using (Tiff tif = Tiff.Open(tiffFilePath, "r"))
            {
                // Get image dimensions
                FieldValue[] value = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();
                value = tif.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                // Get tile dimensions
                int tileWidth = tif.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = tif.GetField(TiffTag.TILELENGTH)[0].ToInt();

                // Geo-transform array (from GeoTIFF metadata)
                double[] geoTransform = new double[] { -180.00000000000006, 0.00500000000000256, 0, -90.000000000000028, 0, -0.0049999999999990061 };

                // Convert bounding box coordinates to pixel coordinates
                int pixelXMin = (int)((minLon - geoTransform[0]) / geoTransform[1]);
                int pixelYMin = (int)((geoTransform[3] - maxLat) / geoTransform[5]);
                int pixelXMax = (int)((maxLon - geoTransform[0]) / geoTransform[1]);
                int pixelYMax = (int)((geoTransform[3] - minLat) / geoTransform[5]);

                // Clamp pixel coordinates to image dimensions
                pixelXMin = Math.Max(0, pixelXMin);
                pixelYMin = Math.Max(0, pixelYMin);
                pixelXMax = Math.Min(width, pixelXMax);
                pixelYMax = Math.Min(height, pixelYMax);

                // Calculate the size of the cropped region
                int cropWidth = pixelXMax - pixelXMin;
                int cropHeight = pixelYMax - pixelYMin;

                // Determine the range of tiles to process
                int tileXMin = pixelXMin / tileWidth;
                int tileYMin = pixelYMin / tileHeight;
                int tileXMax = pixelXMax / tileWidth;
                int tileYMax = pixelYMax / tileHeight;

                // Create a bitmap for the cropped area (256x256)
                using (Bitmap bmp = new Bitmap(256, 256, PixelFormat.Format32bppRgb))
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                    int bytesPerPixel = 3;  // Assuming 24-bit RGB TIFF

                    // Calculate scaling factors for downscaling the crop area to 256x256
                    float scaleX = (float)cropWidth / 256;
                    float scaleY = (float)cropHeight / 256;

                    // Loop through the relevant tiles
                    for (int tileY = tileYMin; tileY <= tileYMax; tileY++)
                    {
                        for (int tileX = tileXMin; tileX <= tileXMax; tileX++)
                        {
                            // Compute the tile index
                            int tileIndex = tif.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);

                            // Read the tile data
                            byte[] tileBuffer = new byte[tif.TileSize()];
                            if (tif.ReadEncodedTile(tileIndex, tileBuffer, 0, tileBuffer.Length) == -1)
                                continue; // Skip the tile if it can't be read

                            // Loop through the pixels in the current tile
                            for (int y = 0; y < tileHeight; y++)
                            {
                                for (int x = 0; x < tileWidth; x++)
                                {
                                    // Calculate the source pixel position in the full image
                                    int srcX = tileX * tileWidth + x;
                                    int srcY = tileY * tileHeight + y;

                                    // Check if the source pixel is within the bounding box
                                    if (srcX < pixelXMin || srcX >= pixelXMax || srcY < pixelYMin || srcY >= pixelYMax)
                                        continue; // Skip if out of bounding box

                                    // Determine the corresponding pixel in the output 256x256 image
                                    int destX = (int)((srcX - pixelXMin) / scaleX);
                                    int destY = (int)((srcY - pixelYMin) / scaleY);

                                    if (destX < 0 || destX >= 256 || destY < 0 || destY >= 256)
                                        continue; // Skip if out of output image bounds

                                    // Get the pixel data from the tile buffer
                                    int srcOffset = (y * tileWidth + x) * bytesPerPixel;
                                    byte r = tileBuffer[srcOffset];
                                    byte g = tileBuffer[srcOffset + 1];
                                    byte b = tileBuffer[srcOffset + 2];

                                    // Write the pixel to the destination bitmap buffer
                                    int destOffset = (destY * bmpData.Stride) + (destX * bytesPerPixel);
                                    IntPtr destPtr = bmpData.Scan0 + destOffset;
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr, r);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr + 1, g);
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr + 2, b);
                                }
                            }
                        }
                    }

                    // Unlock the bitmap bits (after all tiles are processed)
                    bmp.UnlockBits(bmpData);

                    // Save the final output image
                    bmp.Save(outputFilePath, ImageFormat.Png);
                    Console.WriteLine("Cropped GeoTIFF saved successfully!");
                }
            }
        }

        public static (int tileX, int tileY, short level) QuadKeyToTileXY(string quadKey)
        {
            int tileX = 0, tileY = 0;
            short level = (short)quadKey.Length;

            for (int i = level; i > 0; i--)
            {
                int mask = 1 << (i - 1);
                switch (quadKey[level - i])
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
            return (tileX, tileY, level);
        }

        public static (double lonMin, double latMin, double lonMax, double latMax) TileXYToBoundingBox(int tileX, int tileY, int level)
        {
            double n = Math.Pow(2.0, level);

            double lonMin = tileX / n * 360.0 - 180.0;
            double lonMax = (tileX + 1) / n * 360.0 - 180.0;

            double latMinRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
            double latMaxRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n)));

            double latMin = latMinRad * (180.0 / Math.PI);
            double latMax = latMaxRad * (180.0 / Math.PI);

            return (lonMin, latMin, lonMax, latMax);
        }
    }
}

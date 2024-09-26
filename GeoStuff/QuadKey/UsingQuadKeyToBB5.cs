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
                int width = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int tileWidth = tif.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = tif.GetField(TiffTag.TILELENGTH)[0].ToInt();

                // Geo-transform array (should be read from the TIFF metadata in a real-world scenario)
                double[] geoTransform = new double[] { -180.0, 0.005, 0, -90.0, 0, -0.005 };

                // Calculate pixel coordinates
                int pixelXMin = (int)((minLon - geoTransform[0]) / geoTransform[1]);
                int pixelYMin = (int)((geoTransform[3] - maxLat) / geoTransform[5]);
                int pixelXMax = (int)((maxLon - geoTransform[0]) / geoTransform[1]);
                int pixelYMax = (int)((geoTransform[3] - minLat) / geoTransform[5]);

                // Clamp coordinates to image bounds
                pixelXMin = Math.Max(0, pixelXMin);
                pixelYMin = Math.Max(0, pixelYMin);
                pixelXMax = Math.Min(width, pixelXMax);
                pixelYMax = Math.Min(height, pixelYMax);

                int cropWidth = pixelXMax - pixelXMin;
                int cropHeight = pixelYMax - pixelYMin;

                // Create the output bitmap
                using (Bitmap bmp = new Bitmap(256, 256, PixelFormat.Format32bppRgb))
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                    // Get the number of samples per pixel and bits per sample (for 32-bit float, this should be 32)
                    int samplesPerPixel = tif.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                    int bitsPerSample = tif.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

                    if (bitsPerSample != 32)
                    {
                        throw new InvalidOperationException("This code is designed for 32-bit floating point TIFF files.");
                    }

                    // Calculate scaling factors
                    float scaleX = (float)cropWidth / 256;
                    float scaleY = (float)cropHeight / 256;

                    // Loop through the tiles in the TIFF
                    for (int tileY = pixelYMin / tileHeight; tileY <= pixelYMax / tileHeight; tileY++)
                    {
                        for (int tileX = pixelXMin / tileWidth; tileX <= pixelXMax / tileWidth; tileX++)
                        {
                            // Read the tile
                            byte[] tileBuffer = new byte[tif.TileSize()];
                            int tileIndex = tif.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);
                            if (tif.ReadEncodedTile(tileIndex, tileBuffer, 0, tileBuffer.Length) == -1)
                                continue;

                            // Process the tile pixel by pixel
                            for (int y = 0; y < tileHeight; y++)
                            {
                                for (int x = 0; x < tileWidth; x++)
                                {
                                    int srcX = tileX * tileWidth + x;
                                    int srcY = tileY * tileHeight + y;

                                    if (srcX < pixelXMin || srcX >= pixelXMax || srcY < pixelYMin || srcY >= pixelYMax)
                                        continue;

                                    int destX = (int)((srcX - pixelXMin) / scaleX);
                                    int destY = (int)((srcY - pixelYMin) / scaleY);

                                    if (destX < 0 || destX >= 256 || destY < 0 || destY >= 256)
                                        continue;

                                    // Read the 32-bit float value from the tile buffer
                                    int offset = (y * tileWidth + x) * 4; // 4 bytes per float
                                    float value = BitConverter.ToSingle(tileBuffer, offset);

                                    // Normalize the value (this step depends on the data range, here we assume values between 0 and 1)
                                    byte intensity = (byte)(Clamp(value, 0f, 1f) * 255);

                                    // Set the pixel in the output image (we're using grayscale for simplicity)
                                    IntPtr destPtr = bmpData.Scan0 + destY * bmpData.Stride + destX * 4;
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr, intensity); // R
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr + 1, intensity); // G
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr + 2, intensity); // B
                                    System.Runtime.InteropServices.Marshal.WriteByte(destPtr + 3, 255); // A (opaque)
                                }
                            }
                        }
                    }

                    // Unlock and save the bitmap
                    bmp.UnlockBits(bmpData);
                    bmp.Save(outputFilePath, ImageFormat.Png);
                    Console.WriteLine("Cropped 32-bit floating point GeoTIFF saved successfully.");
                }
            }
        }
        // Implement a custom Clamp function
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
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

        public static (double north, double south, double east, double west) TileXYToBoundingBox(int tileX, int tileY, int level)
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

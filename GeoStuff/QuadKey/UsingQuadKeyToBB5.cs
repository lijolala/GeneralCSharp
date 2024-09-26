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
                // Create a bitmap to hold the cropped image
                using (Bitmap bmp = new Bitmap(cropWidth, cropHeight, PixelFormat.Format32bppRgb))
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, cropWidth, cropHeight),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                    // Read and process the TIFF data row by row (strip-wise or tile-wise depending on the TIFF layout)
                    for (int row = pixelYMin; row < pixelYMax; row++)
                    {
                        byte[] scanline = new byte[tif.ScanlineSize()];
                        tif.ReadScanline(scanline, row);

                        // Copy the relevant part of the scanline to the output image
                        for (int col = pixelXMin; col < pixelXMax; col++)
                        {
                            int destCol = col - pixelXMin;
                            int offset = destCol * 4; // Assuming 32bpp RGB image (4 bytes per pixel)
                            int srcOffset = (col - pixelXMin) * 4;

                            // Set the pixel color in the destination bitmap
                            IntPtr destPtr = bmpData.Scan0 + (row - pixelYMin) * bmpData.Stride + destCol * 4;
                            System.Runtime.InteropServices.Marshal.Copy(scanline, srcOffset, destPtr, 4);
                        }
                    }
                    bmp.UnlockBits(bmpData);

                    // Save the cropped image as a new TIFF or PNG file
                    bmp.Save(outputFilePath, ImageFormat.Png);
                    Console.WriteLine("Cropped GeoTIFF saved successfully.");

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

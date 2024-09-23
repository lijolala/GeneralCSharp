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
            // Define the bounding box in WGS84 coordinates (minLon, minLat, maxLon, maxLat)
            //double minLon = -74.0; // Example values
            //double minLat = 40.0;
            //double maxLon = -73.0;
            //double maxLat = 41.0;
            string quadKey = "1202102332";
            string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
            string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;
            var (QTileX, QTileY, level) = QuadKeyToTileXY(quadKey);
            var (minLon, minLat, maxLon, maxLat) = TileXYToBoundingBox(QTileX, QTileY, level);

            // Open the GeoTIFF
            using (Tiff tif = Tiff.Open(@"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif", "r"))
            {
                // Get image dimensions
                FieldValue[] value = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();

                value = tif.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();

                // Get tile dimensions
                int tileWidth = tif.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = tif.GetField(TiffTag.TILELENGTH)[0].ToInt();

                // Geo-transform array
                double[] geoTransform = new double[] { -180.00000000000006, 0.00500000000000256, 0, -90.000000000000028, 0, -0.0049999999999990061 };

                // Convert bounding box coordinates to pixel coordinates
                int pixelXMin = (int)((minLon - geoTransform[0]) / geoTransform[1]);
                int pixelYMin = (int)((geoTransform[3] - maxLat) / geoTransform[5]); // Y is inverted
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

                // Create a bitmap for the cropped area
                using (Bitmap bmp = new Bitmap(cropWidth, cropHeight, PixelFormat.Format24bppRgb))
                {
                    // Lock the bitmap bits to allow fast direct memory operations
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, cropWidth, cropHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    FieldValue[] samplesPerPixelField = tif.GetField(TiffTag.SAMPLESPERPIXEL);
                    int samplesPerPixel = samplesPerPixelField != null ? samplesPerPixelField[0].ToInt() : 1; // Default 1

                    // Get BitsPerSample
                    FieldValue[] bitsPerSampleField = tif.GetField(TiffTag.BITSPERSAMPLE);
                    int bitsPerSample = bitsPerSampleField != null ? bitsPerSampleField[0].ToInt() : 8; // Default 8

                    // Calculate bytes per pixel
                    int bytesPerPixel = (samplesPerPixel * bitsPerSample) / 8;
                  //int bytesPerPixel = 3; // For 24bpp RGB

                    // Process each tile that overlaps with the bounding box
                    for (int tileY = tileYMin; tileY <= tileYMax; tileY++)
                    {
                        for (int tileX = tileXMin; tileX <= tileXMax; tileX++)
                        {
                            // Read the tile data
                            int tileIndex = tif.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);
                            byte[] tileBuffer = new byte[tif.TileSize()];
                            tif.ReadEncodedTile(tileIndex, tileBuffer, 0, tileBuffer.Length);

                            // Determine the region of this tile that intersects the bounding box
                            int srcXStart = Math.Max(pixelXMin - (tileX * tileWidth), 0);
                            int srcYStart = Math.Max(pixelYMin - (tileY * tileHeight), 0);
                            int srcXEnd = Math.Min(tileWidth, pixelXMax - (tileX * tileWidth));
                            int srcYEnd = Math.Min(tileHeight, pixelYMax - (tileY * tileHeight));

                            // Calculate the destination position in the bitmap
                            int destXStart = Math.Max((tileX * tileWidth) - pixelXMin, 0);
                            int destYStart = Math.Max((tileY * tileHeight) - pixelYMin, 0);

                            // Copy the intersecting portion of the tile to the output bitmap
                            for (int y = srcYStart; y < srcYEnd; y++)
                            {
                                int tileOffset = (y * tileWidth + srcXStart) * bytesPerPixel;
                                int bmpOffset = ((destYStart + y - srcYStart) * bmpData.Stride) + (destXStart * bytesPerPixel);
                                int length = (srcXEnd - srcXStart) * bytesPerPixel;

                                // Fast memory copy from tileBuffer to bitmap data
                                System.Runtime.InteropServices.Marshal.Copy(tileBuffer, tileOffset, bmpData.Scan0 + bmpOffset, length);
                            }
                        }
                    }

                    // Unlock the bitmap bits
                    bmp.UnlockBits(bmpData);

                    // Save the cropped image
                    bmp.Save(outputFilePath, ImageFormat.Png);
                    Console.WriteLine("Cropped GeoTIFF saved successfully!");
                }
            }

            // Convert QuadKey to Tile Coordinates
            
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

        // Convert Tile Coordinates to Bounding Box (Lat/Lon)
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

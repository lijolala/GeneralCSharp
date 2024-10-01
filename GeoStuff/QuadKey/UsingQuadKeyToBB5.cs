using System;
using System.Drawing;
using System.Drawing.Imaging;

using BitMiracle.LibTiff.Classic;

public class GeoTiffTiledCropper
{
    public static void Main()
    {
        // Path to your GeoTIFF file
        string quadKey = "1202102332";
        string inputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";
        var (tileX, tileY, level) = RasterHelper.QuadKeyToTileXY(quadKey);
        var (minLon, minLat, maxLon, maxLat) = RasterHelper.TileXYToBoundingBox(tileX, tileY, level);

        // Define the bounding box (longitude and latitude)
        //double minLon = -123.5;
        //double minLat = 37.5;
        //double maxLon = -122.5;
        //double maxLat = 38.5;

        // Crop the GeoTIFF
        CropTiledGeoTiff(inputFilePath, outputFilePath, minLon, minLat, maxLon, maxLat);
    }

    public static void CropTiledGeoTiff(string inputFilePath, string outputFilePath, double minLon, double minLat, double maxLon, double maxLat)
    {
        // Open the TIFF file
        using (Tiff tif = Tiff.Open(inputFilePath, "r"))
        {
            if (tif == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }

            // Get image dimensions
            int width = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Get tile dimensions
            int tileWidth = tif.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = tif.GetField(TiffTag.TILELENGTH)[0].ToInt();

         
            // Get geo-transform (affine transformation matrix)
            double[] geoTransform = RasterHelper.GetGeoTransform(tif); // Example values, you should extract actual values from the file

            // Convert bounding box from lat/lon to pixel coordinates
            int pixelXMin = (int)((minLon - geoTransform[0]) / geoTransform[1]);
            int pixelYMin = (int)((geoTransform[3] - maxLat) / -geoTransform[5]);
            int pixelXMax = (int)((maxLon - geoTransform[0]) / geoTransform[1]);
            int pixelYMax = (int)((geoTransform[3] - minLat) / -geoTransform[5]);

            //// Clamp coordinates to the image bounds
            //pixelXMin = Math.Max(0, pixelXMin);
            //pixelYMin = Math.Max(0, pixelYMin);
            //pixelXMax = Math.Min(width, pixelXMax);
            //pixelYMax = Math.Min(height, pixelYMax);

            int cropWidth = Math.Abs(pixelXMax - pixelXMin);
            int cropHeight = Math.Abs(pixelYMax - pixelYMin);

            // Create a bitmap to hold the cropped image
            using (Bitmap bmp = new Bitmap(cropWidth, cropHeight, PixelFormat.Format32bppRgb))
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, cropWidth, cropHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                // Calculate how many tiles we need to process based on bounding box
                int startTileX = pixelXMin / tileWidth;
                int endTileX = pixelXMax / tileWidth;
                int startTileY = pixelYMin / tileHeight;
                int endTileY = pixelYMax / tileHeight;

                // Iterate through the tiles that overlap the bounding box
                for (int tileY = startTileY; tileY <= endTileY; tileY++)
                {
                    for (int tileX = startTileX; tileX <= endTileX; tileX++)
                    {
                        // Calculate the tile index and read the tile
                        byte[] tileBuffer = new byte[tif.TileSize()];
                        int tileIndex = tif.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);
                        if (tif.ReadEncodedTile(tileIndex, tileBuffer, 0, tileBuffer.Length) == -1)
                            continue;

                        // Calculate the pixel range covered by this tile
                        int tilePixelXMin = tileX * tileWidth;
                        int tilePixelYMin = tileY * tileHeight;
                        int tilePixelXMax = tilePixelXMin + tileWidth;
                        int tilePixelYMax = tilePixelYMin + tileHeight;

                        // Determine the overlapping region between the tile and the crop box
                        int overlapXMin = Math.Max(tilePixelXMin, pixelXMin);
                        int overlapYMin = Math.Max(tilePixelYMin, pixelYMin);
                        int overlapXMax = Math.Min(tilePixelXMax, pixelXMax);
                        int overlapYMax = Math.Min(tilePixelYMax, pixelYMax);

                        // Copy the overlapping region from the tile buffer to the output bitmap
                        for (int y = overlapYMin; y < overlapYMax; y++)
                        {
                            for (int x = overlapXMin; x < overlapXMax; x++)
                            {
                                int srcX = x - tilePixelXMin;
                                int srcY = y - tilePixelYMin;
                                int destX = x - pixelXMin;
                                int destY = y - pixelYMin;

                                // Read the pixel value from the tile buffer
                                int srcOffset = (srcY * tileWidth + srcX) * 4;  // 4 bytes per pixel for 32bpp
                                int destOffset = (destY * bmpData.Stride) + destX * 4;

                                IntPtr destPtr = bmpData.Scan0 + destOffset;
                                System.Runtime.InteropServices.Marshal.Copy(tileBuffer, srcOffset, destPtr, 4);  // Copy 4 bytes (RGBA)
                            }
                        }
                    }
                }

                bmp.UnlockBits(bmpData);

                // Save the cropped image as a new TIFF or PNG file
                bmp.Save(outputFilePath, ImageFormat.Png);
                Console.WriteLine("Cropped GeoTIFF saved successfully.");
            }
        }
    }


   
}

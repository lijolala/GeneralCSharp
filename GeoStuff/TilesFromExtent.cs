using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class TiffTileExtractor
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpBoundingBox";
        double minLat = 40.0, minLon = -74.0, maxLat = 41.0, maxLon = -73.0; // Example bounding box
        int targetZoomLevel = 2; // Example zoom level

        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            // Loop through directories to find the target zoom level
            int zoomLevel = 1;
            while (zoomLevel < targetZoomLevel && image.ReadDirectory())
            {
                zoomLevel++;
            }

            if (zoomLevel != targetZoomLevel)
            {
                Console.WriteLine("Target zoom level not found.");
                return;
            }

            // Assuming the GeoTIFF has geographic information
            FieldValue[] modelPixelScaleTag = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            if (modelPixelScaleTag == null || modelTiepointTag == null)
            {
                Console.WriteLine("GeoTIFF does not contain the necessary georeferencing tags.");
                return;
            }

            double[] pixelScale = modelPixelScaleTag[1].ToDoubleArray();
            double[] tiePoints = modelTiepointTag[1].ToDoubleArray();

            // GeoTIFF origin (top-left corner of the image)
            double originX = tiePoints[3];
            double originY = tiePoints[4];
            double pixelSizeX = pixelScale[0];
            double pixelSizeY = pixelScale[1];

            // Convert bounding box to pixel coordinates
            int minX = (int)((minLon - originX) / pixelSizeX);
            int maxX = (int)((maxLon - originX) / pixelSizeX);
            int minY = (int)((originY - maxLat) / -pixelSizeY);
            int maxY = (int)((originY - minLat) / -pixelSizeY);

            // Get tile size
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Calculate the range of tiles that intersect with the bounding box
            int tileStartX = minX / tileWidth;
            int tileEndX = maxX / tileWidth;
            int tileStartY = minY / tileHeight;
            int tileEndY = maxY / tileHeight;

            // Create the output directory
            Directory.CreateDirectory(outputFolder);

            // Allocate a buffer for one tile
            int tileSize = image.TileSize();
            byte[] buffer = new byte[tileSize];

            // Iterate over the tiles within the bounding box
            for (int tileY = tileStartY; tileY <= tileEndY; tileY++)
            {
                for (int tileX = tileStartX; tileX <= tileEndX; tileX++)
                {
                    int tileIndex = image.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);

                    // Read the tile into the buffer
                    image.ReadTile(buffer, 0, tileX * tileWidth, tileY * tileHeight, 0, 0);

                    // Save the tile as a JPEG image
                    SaveTileAsJpeg(buffer, tileWidth, tileHeight, tileX, tileY, outputFolder);
                }
            }

            Console.WriteLine("Finished processing tiles within the bounding box.");
        }
    }

    static void SaveTileAsJpeg(byte[] buffer, int tileWidth, int tileHeight, int tileX, int tileY, string outputFolder)
    {
        // Assuming 32-bit RGBA data in the buffer, create a Bitmap
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Copy the buffer data into the bitmap's pixel buffer
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);

            bitmap.UnlockBits(bmpData);

            // Construct the file name
            string fileName = Path.Combine(outputFolder, $"tile_{tileY}_{tileX}.jpeg");

            // Save the bitmap as a JPEG file
            bitmap.Save(fileName, ImageFormat.Jpeg);
        }
    }
}

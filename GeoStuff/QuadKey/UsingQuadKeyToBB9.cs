using System;
using System.Linq;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main()
    {
        string geoTiffPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";

        // Open the GeoTIFF file
        using (Tiff image = Tiff.Open(geoTiffPath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }

            // Step 1: Read ModelPixelScaleTag (33550) - the scale of the image in geospatial terms
            double[] pixelScale = RasterHelper.GetModelPixelScales(image);
           

            // Step 2: Read ModelTiepointTag (33922) - the location of the top-left corner of the image
            double[] tiePoints = RasterHelper.GetModeTiePoints(image);
           

            // Step 3: Compute geographic bounds
            // The tiepoints give the location of the upper-left corner (tiePoints[3] is longitude, tiePoints[4] is latitude)
            double topLeftLon = tiePoints[3];
            double topLeftLat = tiePoints[4];

            // Image size (in pixels)
            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Pixel scale (meters per pixel or degrees per pixel depending on the map projection)
            double pixelWidth = pixelScale[0];   // Scale in X direction (longitude)
            double pixelHeight = pixelScale[1];  // Scale in Y direction (latitude)

            // Bottom-right corner of the image
            double bottomRightLon = topLeftLon + (imageWidth * pixelWidth);
            double bottomRightLat = topLeftLat - (imageHeight * pixelHeight);

            // Output the bounds
            Console.WriteLine($"Top-left corner: (Lat: {topLeftLat}, Lon: {topLeftLon})");
            Console.WriteLine($"Bottom-right corner: (Lat: {bottomRightLat}, Lon: {bottomRightLon})");
        }
    }
}

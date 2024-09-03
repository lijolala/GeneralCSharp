using System;

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

            // Attempt to read georeferencing information
            FieldValue[] modelPixelScaleTag = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
            FieldValue[] modelTiepointTag = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);

            if (modelPixelScaleTag == null || modelTiepointTag == null)
            {
                Console.WriteLine("GeoTIFF does not contain the necessary georeferencing tags.");
                // Optionally set default values or handle the lack of georeferencing data
                // Example:
                // throw new InvalidOperationException("Georeferencing information is missing");
                return;
            }

            double[] pixelScale = modelPixelScaleTag[1].ToDoubleArray();
            double[] tiePoints = modelTiepointTag[1].ToDoubleArray();

            // Proceed with pixel-to-coordinate conversion logic...

            // Handle tile extraction as before...
        }
    }
}

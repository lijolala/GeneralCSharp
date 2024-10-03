using System;
using BitMiracle.LibTiff.Classic;

using NetVips;

class Program
{
    static void Main(string[] args)
    {
        // Path to your GeoTIFF file
        string quadKey = "1202102332";
        string inputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\WarImage.png";
        var (tileX, tileY, level) = RasterHelper.QuadKeyToTileXY(quadKey);
        var (minLon, minLat, maxLon, maxLat) = RasterHelper.TileXYToBoundingBox(tileX, tileY, level);

        // Load the GeoTIFF image
        var image = Image.NewFromFile(inputFilePath);
        // Extract GeoTIFF metadata
        var vals = image.GetFields();

        double[] tiePoints = new double[10];
        double[] pixelScale = new double[10];

        using (Tiff tif = Tiff.Open(inputFilePath, "r"))
        {
            if (tif == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }

            tiePoints = RasterHelper.GetModeTiePoints(tif);
            pixelScale = RasterHelper.GetModelPixelScales(tif);
        }
        // Tie point (the coordinates of the top-left corner of the image in geographic coordinates)
        double originX = tiePoints[3];  // Longitude at the top-left corner
        double originY = tiePoints[4];  // Latitude at the top-left corner

        // Pixel scale (size of each pixel in geographic units, e.g., degrees or meters)
        double pixelSizeX = pixelScale[0]; // Width of a pixel in map units (e.g., degrees)
        double pixelSizeY = pixelScale[1]; // Height of a pixel in map units (e.g., degrees)



        // Convert bounding box coordinates to pixel coordinates
        //int minXPixel = (int)((minLon - originX) / pixelSizeX);
        //int maxXPixel = (int)((maxLon - originX) / pixelSizeX);
        //int minYPixel = (int)((originY - maxLat) / pixelSizeY);  // Inverted Y-axis for images
        //int maxYPixel = (int)((originY - minLat) / pixelSizeY);

        var (minXPixel, minYPixel) = RasterHelper.MapToPixel(minLon, minLat, originX, originY, pixelSizeX, pixelSizeY);
        var (maxXPixel, maxYPixel) = RasterHelper.MapToPixel(maxLon, maxLat, originX, originY, pixelSizeX, pixelSizeY);

        // Crop the image by pixel coordinates
        int cropWidth = Math.Abs(maxXPixel - minXPixel);
        int cropHeight = Math.Abs(maxYPixel - minYPixel);
        var croppedImage = image.ExtractArea(minXPixel, minYPixel, cropWidth, cropHeight);

        // Save the cropped image as PNG
        croppedImage.WriteToFile(outputFilePath);

        Console.WriteLine($"Cropped image saved to {outputFilePath}");
    }
}
using System;

using BitMiracle.LibTiff.Classic;

using NetVips;

class Program
{
    static void Main(string[] args)
    {
        // Path to the generated image from the GeoTIFF (e.g., PNG)
        //string inputImagePath = @"path\to\your\generated_image.png";
        //string outputCroppedPath = @"path\to\your\cropped_output.png";
        string quadKey = "1202102332";
        string inputImagePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\WarImage1.png";
        var (tileX, tileY, level) = RasterHelper.QuadKeyToTileXY(quadKey);
        var (minLon, minLat, maxLon, maxLat) = RasterHelper.TileXYToBoundingBox(tileX, tileY, level);

        //// Define the bounding box in geographic coordinates (replace with your actual coordinates)
        //double minLon = 10.0;  // Minimum longitude (left)
        //double minLat = 50.0;  // Minimum latitude (bottom)
        //double maxLon = 20.0;  // Maximum longitude (right)
        //double maxLat = 60.0;  // Maximum latitude (top)

        // Metadata of the original GeoTIFF (extract this from the GeoTIFF you used to generate the image)
        // Example values - replace these with your actual metadata
        double originX = 5.0;  // Longitude of the top-left corner of the image
        double originY = 70.0; // Latitude of the top-left corner of the image
        double pixelSizeX = 0.0001;  // Degrees/pixel for longitude
        double pixelSizeY = 0.0001;  // Degrees/pixel for latitude

        double[] tiePoints = new double[10];
        double[] pixelScale = new double[10];

        int mapWidth, mapHeight = 0;

        using (Tiff tif = Tiff.Open(inputImagePath, "r"))
        {
            if (tif == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }

            tiePoints = RasterHelper.GetModeTiePoints(tif);
            pixelScale = RasterHelper.GetModelPixelScales(tif);

            // Get the image width and height
            mapWidth = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            mapHeight = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
        }

        originX = tiePoints[0];
        originY = tiePoints[1];

        pixelSizeX = pixelScale[0];
        pixelSizeY = pixelScale[1];

        double mapLonDelta = maxLon - minLon;
        double mapLatBottomDegree = minLat * Math.PI / 180;

        var pixelPosition = ConvertGeoToPixel(maxLat, maxLon, mapWidth, mapHeight, minLon, mapLonDelta, mapLatBottomDegree);
        Console.WriteLine($"x: {pixelPosition.Item1}, y: {pixelPosition.Item2}");

        // Load the generated image (e.g., PNG or other formats)
        var image = Image.NewFromFile(inputImagePath);

        // Convert geographic bounding box to pixel coordinates
        int minXPixel = (int)((minLon - originX) / pixelSizeX);
        int maxXPixel = (int)((maxLon - originX) / pixelSizeX);
        int minYPixel = (int)((originY - maxLat) / pixelSizeY);  // Y-axis is inverted for images
        int maxYPixel = (int)((originY - minLat) / pixelSizeY);

        // Ensure the pixel values are within the image boundaries
        minXPixel = Math.Max(0, minXPixel);
        maxXPixel = Math.Min(image.Width - 1, maxXPixel);
        minYPixel = Math.Max(0, minYPixel);
        maxYPixel = Math.Min(image.Height - 1, maxYPixel);

        // Calculate the width and height for cropping
        int cropWidth = maxXPixel - minXPixel;
        int cropHeight = maxYPixel - minYPixel;

        // Crop the image using the calculated pixel coordinates
        var croppedImage = image.ExtractArea(minXPixel, minYPixel, cropWidth, cropHeight);

        // Save the cropped image
        croppedImage.WriteToFile(outputFilePath);

        Console.WriteLine($"Cropped image saved to {outputFilePath}");
    }

    static Tuple<double, double> ConvertGeoToPixel(double lat, double lon, double mapWidth, double mapHeight, double mapLonLeft, double mapLonDelta, double mapLatBottomDegree)
    {
        // Convert longitude to x coordinate
        double x = (lon - mapLonLeft) * (mapWidth / mapLonDelta);

        // Convert latitude to y coordinate
        lat = lat * Math.PI / 180;
        double worldMapWidth = ((mapWidth / mapLonDelta) * 360) / (2 * Math.PI);
        double mapOffsetY = (worldMapWidth / 2 * Math.Log((1 + Math.Sin(mapLatBottomDegree)) / (1 - Math.Sin(mapLatBottomDegree))));
        double y = mapHeight - ((worldMapWidth / 2 * Math.Log((1 + Math.Sin(lat)) / (1 - Math.Sin(lat)))) - mapOffsetY);

        return Tuple.Create(x, y);
    }
}

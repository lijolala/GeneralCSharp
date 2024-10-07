using System;
using System.Drawing;

public class Program
{
    const int TileSize = 256; // Standard tile size (256x256 pixels)

    public static void Main()
    {
        // Geographic coordinates (bounding box)
        double topLeftLat = -59;  // Latitude for New York City (top-left corner)
        double topLeftLon = -29; // Longitude for New York City (top-left corner)
        double bottomRightLat = -31; // Latitude for Statue of Liberty (bottom-right corner)
        double bottomRightLon = -56; // Longitude for Statue of Liberty (bottom-right corner)
        int zoomLevel = 8;          // Zoom level

        // Load the original map image
        Bitmap sourceImage = new Bitmap(@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\WarImage.png");

        // The actual size of the source image (in pixels)
        int imageWidth = sourceImage.Width;
        int imageHeight = sourceImage.Height;

        // Step 1: Convert geographic coordinates to pixel coordinates at the given zoom level
        var topLeft = LatLonToPixel(topLeftLat, topLeftLon, zoomLevel);
        var bottomRight = LatLonToPixel(bottomRightLat, bottomRightLon, zoomLevel);

        // Step 2: Map size at the given zoom level
        double fullMapWidth = TileSize * Math.Pow(2, zoomLevel); // Full world map width
        double fullMapHeight = fullMapWidth; // Full map height (square map)

        // Step 3: Scale pixel coordinates to the size of the source image
        double scaleX = (double)imageWidth / fullMapWidth;
        double scaleY = (double)imageHeight / fullMapHeight;

        int scaledX1 = (int)(topLeft.x * scaleX);
        int scaledY1 = (int)(topLeft.y * scaleY);
        int scaledX2 = (int)(bottomRight.x * scaleX);
        int scaledY2 = (int)(bottomRight.y * scaleY);

        // Step 4: Calculate the cropping region
        int cropWidth = Math.Abs(scaledX2 - scaledX1);
        int cropHeight = Math.Abs(scaledY2 - scaledY1);

        Rectangle cropArea = new Rectangle(scaledX1, scaledY1, cropWidth, cropHeight);

        // Ensure the crop area is within the image bounds
        cropArea.Intersect(new Rectangle(0, 0, imageWidth, imageHeight));

        // Step 5: Crop the image
        Bitmap croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
        using (Graphics g = Graphics.FromImage(croppedImage))
        {
            g.DrawImage(sourceImage, new Rectangle(0, 0, cropArea.Width, cropArea.Height), cropArea, GraphicsUnit.Pixel);
        }

        // Step 6: Resize the cropped image to 256x256 pixels
        Bitmap resizedImage = new Bitmap(croppedImage, new Size(256, 256));
        resizedImage.Save(@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\WarImageCrop.png");

        Console.WriteLine("Image cropped and resized successfully.");
    }

    // Converts latitude to pixel Y coordinate at a given zoom level
    public static (double x, double y) LatLonToPixel(double latitude, double longitude, int zoomLevel)
    {
        double sinLat = Math.Sin(DegreesToRadians(latitude));
        double pixelY = (0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * TileSize * Math.Pow(2, zoomLevel);
        double pixelX = ((longitude + 180.0) / 360.0) * TileSize * Math.Pow(2, zoomLevel);
        return (pixelX, pixelY);
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}

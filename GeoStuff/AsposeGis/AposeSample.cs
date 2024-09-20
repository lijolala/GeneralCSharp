using System;
using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Tiff;
using Aspose.Imaging.FileFormats.Tiff.Enums;
using Aspose.Imaging.ImageOptions;
using Aspose.Gis;  // Main namespace for geospatial operations
class Program
{
    static void Main(string[] args)
    {
        // Load the GeoTIFF image using Aspose.Imaging
       // string rasterFilePath = "your-geotiff-file.tif";
        string quadKey = "1202102332";
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;
        var (tileX, tileY, level) = QuadKeyToTileXY(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileXYToBoundingBox(tileX, tileY, level);

        // Step 3: Extract the part of the image using the bounding box
        ExtractGeoTiffTile(tiffFilePath, minLat, minLon, maxLat, maxLon);

        using (TiffImage tiffImage = (TiffImage)Image.Load(tiffFilePath))
        {
            // Step 1: Manually define the GeoTransform matrix (obtained externally)
            // Example affine transform: [origin_x, pixel_size_x, 0, origin_y, 0, pixel_size_y]
            double[] geoTransform = new double[] { -180.00000000000006, 0.00500000000000256, 0, -90.000000000000028, 0, -0.0049999999999990061 };
            
            // Step 3: Convert Lat/Lon to pixel coordinates using the geoTransform
            (int minX, int minY) = LatLonToPixel(minLat, minLon, geoTransform);
            (int maxX, int maxY) = LatLonToPixel(maxLat, maxLon, geoTransform);

            // Define the width and height of the tile (256x256 pixels)
            int tileWidth = 256;
            int tileHeight = 256;
            // Center the 256x256 pixel tile around the middle of the bounding box
            int centerX = (minX + maxX) / 2;
            int centerY = (minY + maxY) / 2;

            // Calculate the top-left corner of the tile (centered on the bounding box)
            int tileMinX = Math.Abs(centerX - tileWidth / 2);
            int tileMinY = Math.Abs(centerY - tileHeight / 2);

            Aspose.Imaging.Rectangle boundingBox = new Aspose.Imaging.Rectangle(tileMinX, tileMinY, tileWidth, tileHeight);

            foreach (TiffFrame frame in tiffImage.Frames)
            {
                // Crop the current frame using the bounding box
                frame.Crop(boundingBox);
            }

            // Step 5: Save the cropped image
            tiffImage.Save(outputFilePath, new TiffOptions(TiffExpectedFormat.TiffLzwRgb));

            Console.WriteLine("Cropped image saved successfully.");
        }
    }

    // Step 3: Extract the tile (cropped image) from GeoTIFF based on the bounding box
    public static void ExtractGeoTiffTile(string geoTiffFilePath, double minLat, double minLon, double maxLat, double maxLon)
    {
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";
        using (var image = Aspose.Imaging.Image.Load(geoTiffFilePath) as TiffImage)
        {
            if (image == null)
            {
                Console.WriteLine("Failed to load GeoTIFF image.");
                return;
            }

            // Get image dimensions
            int imageWidth = image.Width;
            int imageHeight = image.Height;

            // Assuming the GeoTIFF spans the full extent of the world (-180 to 180 longitude, -90 to 90 latitude)
            double worldMinLon = -180.0;
            double worldMaxLon = 180.0;
            double worldMinLat = -90.0;
            double worldMaxLat = 90.0;

            // Convert the geographic bounding box to pixel coordinates within the GeoTIFF
            int minXPixel = (int)((minLon - worldMinLon) / (worldMaxLon - worldMinLon) * imageWidth);
            int maxXPixel = (int)((maxLon - worldMinLon) / (worldMaxLon - worldMinLon) * imageWidth);
            int minYPixel = (int)((worldMaxLat - maxLat) / (worldMaxLat - worldMinLat) * imageHeight); // Flip Y axis for latitudes
            int maxYPixel = (int)((worldMaxLat - minLat) / (worldMaxLat - worldMinLat) * imageHeight);

            // Define the rectangle area to crop
            var croppedRect = new Rectangle(minXPixel, minYPixel, Math.Abs(maxXPixel - minXPixel), Math.Abs(maxYPixel - minYPixel));
            image.Crop(croppedRect) ;
           image.Save(outputFilePath, new PngOptions());
                //if (croppedImage != null)
                //{
                //    string outputPath = "extracted-tile.png";  // Save the cropped tile
                //    croppedImage.Save(outputPath, new PngOptions());
                //    Console.WriteLine($"Tile extracted and saved as {outputPath}");
                //}
                //else
                //{
                //    Console.WriteLine("Failed to crop the image.");
                //}
           
        }
    }

    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, int level) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int level = quadKey.Length;

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

    // Helper function to convert lat/lon to pixel coordinates using the GeoTransform matrix
    public static (int, int) LatLonToPixel(double lat, double lon, double[] geoTransform)
    {
        // GeoTransform[0] - Top-left x (Longitude of upper-left corner)
        // GeoTransform[1] - Pixel size in x direction (Longitude per pixel)
        // GeoTransform[3] - Top-left y (Latitude of upper-left corner)
        // GeoTransform[5] - Pixel size in y direction (Latitude per pixel)

        double pixelX = (lon - geoTransform[0]) / geoTransform[1];
        double pixelY = (lat - geoTransform[3]) / geoTransform[5];

        return ((int)pixelX, (int)pixelY);
    }
}

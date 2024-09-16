using System;
using BitMiracle.LibTiff.Classic;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main()
    {
        // Path to the GeoTIFF file
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputTilePath =
            @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";
        // QuadKey for the tile you want to extract
        string quadKey = "02301012130";

        // Convert the quadKey to a lat/lon bounding box
        BoundingBox boundingBox = QuadKeyToBoundingBox(quadKey);

        // Calculate the zoom level from the length of the quadKey
        int zoomLevel = quadKey.Length;

        // Open the GeoTIFF file
        using (Tiff image = Tiff.Open(tiffFilePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the GeoTIFF file.");
                return;
            }
            int numberOfDirectories = image.NumberOfDirectories();

            // Example mapping: assuming zoom level maps directly to IFD index
            if (zoomLevel < numberOfDirectories) {
                image.SetDirectory((short)zoomLevel);
            }else {
                image.SetDirectory((short)numberOfDirectories);
            }
            // Get image width and height
            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Get the tile size
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Get the GeoTransform tag (6 values)
            double[] geoTransform = GetGeoTransform(image);
          

            // Get the raster pixel data
            int[] raster = new int[image.TileSize()];
            image.ReadRGBAImage(tileWidth, tileHeight, raster);

            // Convert bounding box to pixel coordinates
            Rectangle quadKeyPixelRect = BoundingBoxToPixelRect(boundingBox, geoTransform, tileWidth, tileHeight, zoomLevel);

            // Extract the relevant portion of the image
            Bitmap extractedImage = ExtractQuadKeyImage(raster, tileWidth, tileHeight, quadKeyPixelRect, zoomLevel);

            // Save the extracted image
            extractedImage.Save(outputTilePath + $"\\extracted_quadKey_image_zoom_{zoomLevel}.png", ImageFormat.Png);
            Console.WriteLine($"Image extracted successfully for zoom level {zoomLevel}.");
        }
    }
    public static double[] GetGeoTransform(Tiff image)
    {
        // ModelTiepointTag (tag ID: 33922) and ModelPixelScaleTag (tag ID: 33550)
        const int ModelTiepointTag = 33922;
        const int ModelPixelScaleTag = 33550;

        FieldValue[] pixelScaleValues = image.GetField((TiffTag)ModelPixelScaleTag);
        double[] pixelScales = { 1.0, 1.0 }; // Default pixel scales

        if (pixelScaleValues != null && pixelScaleValues.Length > 0)
        {
            byte[] byteArray = pixelScaleValues[1].GetBytes();
            pixelScales = ByteArrayToDoubleArray(byteArray);
        }

        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues == null || tiePointsValues.Length == 0)
            throw new Exception("ModelTiepointTag not found in the GeoTIFF.");

        byte[] tiePointsArray = tiePointsValues[1].GetBytes();
        double[] tiePoints = ByteArrayToDoubleArray(tiePointsArray);

        double[] geoTransform = new double[6];
        geoTransform[0] = tiePoints[3]; // Top-left X
        geoTransform[1] = pixelScales[0]; // Pixel width
        geoTransform[3] = tiePoints[4]; // Top-left Y
        geoTransform[5] = -pixelScales[1]; // Pixel height

        return geoTransform;
    }
    static double[] ByteArrayToDoubleArray(byte[] byteArray)
    {
        int doubleSize = sizeof(double);
        int count = byteArray.Length / doubleSize;
        double[] result = new double[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToDouble(byteArray, i * doubleSize);
        }

        return result;
    }

    // Converts a quadKey to a geographic bounding box (lat/lon)
    static BoundingBox QuadKeyToBoundingBox(string quadKey)
    {
        int tileX = 0, tileY = 0, levelOfDetail = quadKey.Length;

        for (int i = levelOfDetail; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[levelOfDetail - i])
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
                    throw new ArgumentException("Invalid quadKey digit sequence.");
            }
        }

        double n = Math.Pow(2, levelOfDetail);
        double minLon = tileX / n * 360.0 - 180.0;
        double maxLon = (tileX + 1) / n * 360.0 - 180.0;
        double minLat = TileYToLatitude(tileY + 1, levelOfDetail);
        double maxLat = TileYToLatitude(tileY, levelOfDetail);

        return new BoundingBox
        {
            MinLat = minLat,
            MinLon = minLon,
            MaxLat = maxLat,
            MaxLon = maxLon
        };
    }

    // Converts tile Y to latitude
    static double TileYToLatitude(int tileY, int levelOfDetail)
    {
        double n = Math.PI - 2.0 * Math.PI * tileY / Math.Pow(2.0, levelOfDetail);
        return 180.0 / Math.PI * Math.Atan(Math.Sinh(n));
    }

    // Converts geographic bounding box to pixel coordinates and adjusts for zoom level
    static Rectangle BoundingBoxToPixelRect(BoundingBox bbox, double[] geoTransform, int tileWidth, int tileHeight, int zoomLevel)
    {
       

        // Calculate pixel coordinates based on the zoom level
        int minX = (int)((bbox.MinLon - geoTransform[0]) / geoTransform[1]);
        int maxX = (int)((bbox.MaxLon - geoTransform[0]) / geoTransform[1]);
        int minY = (int)((geoTransform[3] - bbox.MaxLat) / Math.Abs(geoTransform[5]));
        int maxY = (int)((geoTransform[3] - bbox.MinLat) / Math.Abs(geoTransform[5]));


        minX = Math.Max(minX, 0);
        minY = Math.Max(minY, 0);

        return new Rectangle(minX, minY, tileWidth, tileHeight);
    }
    static Bitmap ExtractQuadKeyImage(int[] raster, int width, int height, Rectangle quadKeyRect, int zoomLevel)
    {
        Bitmap extractedImage = new Bitmap(quadKeyRect.Width, quadKeyRect.Height);

        for (int y = 0; y < quadKeyRect.Height; y++)
        {
            for (int x = 0; x < quadKeyRect.Width; x++)
            {
                int pixelIndex = (quadKeyRect.Y + y) * width + (quadKeyRect.X + x);

                // Ensure that the pixelIndex is within the bounds of the raster array
                if (pixelIndex >= 0 && pixelIndex < raster.Length)
                {
                    int pixel = raster[pixelIndex];

                    // Extract color channels
                    Color color = Color.FromArgb(
                        (pixel >> 24) & 0xff, // Alpha
                        (pixel >> 16) & 0xff, // Red
                        (pixel >> 8) & 0xff,  // Green
                        pixel & 0xff);        // Blue

                    extractedImage.SetPixel(x, y, color);
                }
                else
                {
                    // If the pixelIndex is out of bounds, fill with a transparent pixel (optional)
                    extractedImage.SetPixel(x, y, Color.Transparent);
                }
            }
        }

        // Optional: scale based on zoom level (if needed)
        int scaleFactor = 1 << (zoomLevel - 1); // Example scaling based on zoom
        if (scaleFactor > 1)
        {
            extractedImage = new Bitmap(extractedImage, new Size(extractedImage.Width / scaleFactor, extractedImage.Height / scaleFactor));
        }

        return extractedImage;
    }

}

// Class to represent a geographic bounding box
class BoundingBox
{
    public double MinLat { get; set; }
    public double MinLon { get; set; }
    public double MaxLat { get; set; }
    public double MaxLon { get; set; }
}

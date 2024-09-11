using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; // Replace with your GeoTIFF file path
        string quadKey = "2101"; // Replace with your QuadKey
        string outputTilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; // Replace with your output tile path

        // Extract tile based on QuadKey
        ExtractTileFromGeoTiff(filePath, quadKey, outputTilePath);
    }

    /// <summary>
    /// Extracts tile from the GeoTIFF based on QuadKey and saves it as a PNG.
    /// </summary>
    public static void ExtractTileFromGeoTiff(string geoTiffPath, string quadKey, string outputTilePath)
    {
        var (zoomLevel, tileX, tileY) = QuadKeyToTile(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileToBoundingBox(tileX, tileY, zoomLevel);

        using (Tiff image = Tiff.Open(geoTiffPath, "r"))
        {
            // Get the GeoTransform from the GeoTIFF
            double[] geoTransform = GetGeoTransform(image);

            // Check if the GeoTIFF is projected in Web Mercator (EPSG:3857), adjust if needed
            AdjustProjectionIfNeeded(geoTransform);

            // Compute the pixel coordinates in the GeoTIFF based on the bounding box
            int minXPixel, minYPixel, maxXPixel, maxYPixel;
            GeoToPixel(minLon, minLat, geoTransform, out minXPixel, out minYPixel);
            GeoToPixel(maxLon, maxLat, geoTransform, out maxXPixel, out maxYPixel);

            // Example tile size logic; you may need to adjust based on the specific GeoTIFF and its projection
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
            byte[] tileBuffer = new byte[image.TileSize()];

            // Read the tile data from the GeoTIFF
            image.ReadTile(tileBuffer, 0, minXPixel, minYPixel, 0, 0);

            // Save the tile as PNG
            SaveTileAsPng(tileBuffer, tileWidth, tileHeight, outputTilePath);
        }
    }

    /// <summary>
    /// Extracts the GeoTransform matrix from the GeoTIFF.
    /// </summary>
    public static double[] GetGeoTransform(Tiff image)
    {
        // ModelPixelScaleTag is typically tag number 33550
        const int ModelPixelScaleTag = 33550;

        FieldValue[] pixelScaleValues = image.GetField((TiffTag)ModelPixelScaleTag);
        double[] pixelScales = null;
        if (pixelScaleValues != null && pixelScaleValues.Length > 0)
        {
            // The ModelPixelScaleTag contains three values: ScaleX, ScaleY, ScaleZ (Z is often ignored for 2D images)
          //  double scaleX = pixelScaleValues[0].ToDouble(); // Scale in X direction (e.g., meters per pixel)
            byte[] byteArray = pixelScaleValues[1].GetBytes(); // Assuming pixelScaleValues[1] is the byte array
            pixelScales = ByteArrayToDoubleArray(byteArray);

            if (pixelScales.Length >= 2)
            {
                double scaleX = pixelScales[0]; // Scale in X direction (e.g., meters per pixel)
                double scaleY = pixelScales[1]; // Scale in Y direction (e.g., meters per pixel)

                Console.WriteLine($"Pixel Scale - X: {scaleX}, Y: {scaleY}");
            }
            else
            {
                Console.WriteLine("Unexpected pixel scale array length.");
            }
        }
        else
        {
            Console.WriteLine("ModelPixelScaleTag not found in the GeoTIFF.");
        }

        const int ModelTiepointTag = 33922;
        double[] tiePoints = null;
        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues != null && tiePointsValues.Length > 0)
        {
            // Convert the byte array to double array (since ModelTiepointTag is an array of doubles)
            byte[] byteArray = tiePointsValues[1].GetBytes(); // Assuming tiePointsValues[1] is the byte array
            tiePoints = ByteArrayToDoubleArray(byteArray);

            // Print out each tie point (each tie point has 6 values: 3 for pixel coordinates, 3 for geographic coordinates)
            for (int i = 0; i < tiePoints.Length; i += 6)
            {
                double pixelX = tiePoints[i];
                double pixelY = tiePoints[i + 1];
                double pixelZ = tiePoints[i + 2];
                double geoX = tiePoints[i + 3]; // Geographic X (e.g., longitude or easting)
                double geoY = tiePoints[i + 4]; // Geographic Y (e.g., latitude or northing)
                double geoZ = tiePoints[i + 5]; // Geographic Z (usually 0 for 2D images)

                Console.WriteLine($"Tie Point - Pixel (X, Y, Z): ({pixelX}, {pixelY}, {pixelZ}), Geo (X, Y, Z): ({geoX}, {geoY}, {geoZ})");
            }
        }
        else
        {
            Console.WriteLine("ModelTiepointTag not found in the GeoTIFF.");
        }

        // ModelTiePointTag (GeoTIFF tag 33922)
      //  double[] tiePoints = GetTagAsDoubleArray(image, 33922);
        if (tiePoints == null || tiePoints.Length < 6)
            throw new Exception("ModelTiePointTag is missing or incomplete.");

        // GeoTransform format: [topLeftX, pixelWidth, 0, topLeftY, 0, -pixelHeight]
        double[] geoTransform = new double[6];
        geoTransform[0] = tiePoints[3]; // topLeftX (longitude of the upper-left corner)
        if (pixelScales != null)
        {
            geoTransform[1] = pixelScales[0]; // pixelWidth (size of the pixel in the x direction)
            geoTransform[2] = 0; // rotation (not used)
            geoTransform[3] = tiePoints[4]; // topLeftY (latitude of the upper-left corner)
            geoTransform[4] = 0; // rotation (not used)
            geoTransform[5] = -pixelScales[1]; // pixelHeight (negative because Y decreases downwards)
        }

        return geoTransform;
    }

    // Convert byte[] to double[] (assuming each double is 8 bytes)
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
    /// <summary>
    /// Retrieves a tag from the GeoTIFF as a double array.
    /// </summary>
    public static double[] GetTagAsDoubleArray(Tiff image, int tag)
    {
        FieldValue[] field = image.GetField((TiffTag)tag);
        if (field == null)
            return null;

        double[] values = new double[field.Length];
        for (int i = 0; i < field.Length; i++)
        {
            values[i] = field[i].ToDouble();
        }

        return values;
    }
    /// <summary>
    /// Converts geographic coordinates (longitude/latitude) to pixel coordinates using the GeoTransform.
    /// </summary>
    public static void GeoToPixel(double lon, double lat, double[] geoTransform, out int pixelX, out int pixelY)
    {
        pixelX = (int)((lon - geoTransform[0]) / geoTransform[1]);
        pixelY = (int)((lat - geoTransform[3]) / geoTransform[5]);
    }

    /// <summary>
    /// Adjusts the projection if necessary to Web Mercator (EPSG:3857).
    /// </summary>
    public static void AdjustProjectionIfNeeded(double[] geoTransform)
    {
        // Check the projection metadata in the GeoTIFF and apply necessary transformations.
        // This is where you would change the projection if the file is not in Web Mercator.

        // For now, assuming it's already in Web Mercator (EPSG:3857).
    }

    /// <summary>
    /// Converts the tile buffer into a PNG image and saves it to a file.
    /// </summary>
    public static void SaveTileAsPng(byte[] buffer, int tileWidth, int tileHeight, string outputPath)
    {
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            // Lock the bitmap's bits and copy the buffer into the bitmap
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight),
                                                  ImageLockMode.WriteOnly, bitmap.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            // Set transparency (optional)
            SetBitmapTransparency(bitmap);

            // Save the bitmap as a PNG file
            bitmap.Save(outputPath, ImageFormat.Png);
        }
    }

    /// <summary>
    /// Applies transparency to the bitmap.
    /// </summary>
    public static void SetBitmapTransparency(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                // Example logic for transparency: make white pixels fully transparent
                if (pixelColor.R == 255 && pixelColor.G == 255 && pixelColor.B == 255)
                {
                    Color transparentColor = Color.FromArgb(0, pixelColor.R, pixelColor.G, pixelColor.B);
                    bitmap.SetPixel(x, y, transparentColor);
                }
            }
        }
    }

    /// <summary>
    /// Converts a QuadKey to tile X and tile Y coordinates at a specific zoom level.
    /// </summary>
    public static (int zoomLevel, int tileX, int tileY) QuadKeyToTile(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int zoomLevel = quadKey.Length;

        for (int i = zoomLevel; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[zoomLevel - i])
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

        return (zoomLevel, tileX, tileY);
    }

    /// <summary>
    /// Converts tile X and tile Y coordinates at a given zoom level into a bounding box (Web Mercator coordinates).
    /// </summary>
    public static (double minLon, double minLat, double maxLon, double maxLat) TileToBoundingBox(int tileX, int tileY, int zoomLevel)
    {
        double n = Math.Pow(2.0, zoomLevel);

        double lon1 = tileX / n * 360.0 - 180.0;
        double lon2 = (tileX + 1) / n * 360.0 - 180.0;
        double lat1 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n))) * (180.0 / Math.PI);
        double lat2 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n))) * (180.0 / Math.PI);

        return (lon1, lat2, lon2, lat1);
    }

}

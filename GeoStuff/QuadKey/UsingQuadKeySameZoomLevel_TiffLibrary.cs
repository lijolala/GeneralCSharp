using System;
using System.Drawing;
using System.Drawing.Imaging;

using TiffLibrary;

class GeoTiffTileExtractor
{
    // Converts QuadKey to tile X, tile Y, and zoom level
    public static (int tileX, int tileY, int zoom) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int zoom = quadKey.Length;

        for (int i = zoom; i > 0; i--)
        {
            int bitmask = 1 << (i - 1);
            switch (quadKey[zoom - i])
            {
                case '0':
                    break;
                case '1':
                    tileX |= bitmask;
                    break;
                case '2':
                    tileY |= bitmask;
                    break;
                case '3':
                    tileX |= bitmask;
                    tileY |= bitmask;
                    break;
                default:
                    throw new ArgumentException("Invalid QuadKey.");
            }
        }

        return (tileX, tileY, zoom);
    }

    // Open GeoTIFF and return the appropriate ImageFileDirectory (IFD) for the given zoom level
    public static TiffImageFileDirectory GetGeoTiffDirectoryForZoom(string filePath, int zoomLevel)
    {
        using (var tiff = Tiff.Open(filePath))
        {
            int numberOfDirectories = tiff.ImageFileDirectories.Count;

            // Example mapping: assuming zoom level maps directly to IFD index
            if (zoomLevel < numberOfDirectories)
            {
                return tiff.GetImageFileDirectory(zoomLevel);
            }

            throw new Exception("No matching zoom level found in GeoTIFF.");
        }
    }

    // Converts tile coordinates to pixel coordinates based on GeoTransform
    public static (int pixelX, int pixelY) TileToPixelCoordinates(int tileX, int tileY, double[] geoTransform, int tileSize)
    {
        double originX = geoTransform[0];
        double pixelWidth = geoTransform[1];  // Pixel size in X direction
        double originY = geoTransform[3];
        double pixelHeight = geoTransform[5]; // Pixel size in Y direction

        double worldX = originX + tileX * tileSize * pixelWidth;
        double worldY = originY + tileY * tileSize * pixelHeight;

        int pixelX = (int)((worldX - originX) / pixelWidth);
        int pixelY = (int)((worldY - originY) / pixelHeight);

        return (pixelX, pixelY);
    }

    // Extract tile from GeoTIFF at given pixel coordinates and save it as Bitmap
    public static Bitmap ExtractTile(TiffFile tiff, int pixelX, int pixelY, int tileSize, TiffImageFileDirectory ifd)
    {
        int width = tileSize;
        int height = tileSize;

        byte[] buffer = new byte[width * height * 4]; // Assuming RGBA 32-bit format

        // Read a portion of the GeoTIFF image
        tiff.ReadTile(buffer, pixelX, pixelY, 0, 0, width, height);

        // Create a Bitmap object
        Bitmap tileBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int bufferIndex = (y * width + x) * 4;
                Color pixelColor = Color.FromArgb(buffer[bufferIndex + 3], buffer[bufferIndex + 2], buffer[bufferIndex + 1], buffer[bufferIndex]);
                tileBitmap.SetPixel(x, y, pixelColor);
            }
        }

        return tileBitmap;
    }

    // Save extracted tile as an image (e.g., PNG)
    public static void SaveTileAsImage(Bitmap tile, string outputPath)
    {
        tile.Save(outputPath, ImageFormat.Png);
    }

    // Main function to extract the tile for a given QuadKey
    public static void ExtractGeoTiffTileForQuadKey(string quadKey, string geoTiffFilePath, int tileSize, string outputImagePath)
    {
        // Step 1: Convert QuadKey to Tile X, Tile Y, and Zoom Level
        var (tileX, tileY, zoomLevel) = QuadKeyToTileXY(quadKey);

        // Step 2: Open the GeoTIFF file and get the directory corresponding to the zoom level
        TiffImageFileDirectory ifd = GetGeoTiffDirectoryForZoom(geoTiffFilePath, zoomLevel);

        // Step 3: Get the GeoTransform for the selected zoom level
        // Example GeoTransform: [OriginX, PixelWidth, 0, OriginY, 0, -PixelHeight]
        double[] geoTransform = { ifd.BitsPerSample[0], 1, 0, ifd.BitsPerSample[0], 0, -1 };

        // Step 4: Convert Tile coordinates to Pixel coordinates
        var (pixelX, pixelY) = TileToPixelCoordinates(tileX, tileY, geoTransform, tileSize);

        // Step 5: Extract the tile from the GeoTIFF
        using (var tiff = TiffFile.Open(geoTiffFilePath))
        {
            Bitmap tileBitmap = ExtractTile(tiff, pixelX, pixelY, tileSize, ifd);

            // Step 6: Save the extracted tile as an image
            SaveTileAsImage(tileBitmap, outputImagePath);

            Console.WriteLine($"Tile saved at: {outputImagePath}");
        }
    }

    public static void Main(string[] args)
    {
        // Example inputs
        string quadKey = "023010"; // Example QuadKey
        string geoTiffFilePath = @"path\to\your\geotiff.tiff";
        int tileSize = 256; // Example tile size (256x256)
        string outputImagePath = @"path\to\save\output.png";

        // Extract the tile for the given QuadKey and save it as an image
        ExtractGeoTiffTileForQuadKey(quadKey, geoTiffFilePath, tileSize, outputImagePath);
    }
}

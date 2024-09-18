using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class TiffTileReader
{
    int foundZoomLevel = 0;
    static void Main(string[] args) {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1";
        string quadkey = "120210233222";
        int foundZoomLevel = 0;

        // Convert the quadkey to tile coordinates
        var (tileX, tileY, zoomLevel) = QuadKeyToTile(quadkey);

        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            Console.WriteLine($"\nProcessing Quadkey: {quadkey} (Zoom Level {zoomLevel}, X: {tileX}, Y: {tileY}):");

            // Navigate to the appropriate zoom level directory in the TIFF by comparing image dimensions
            bool foundZoom = NavigateToNearestZoomLevelByDimensions(image, zoomLevel, out foundZoomLevel);

            if (!foundZoom)
            {
                Console.WriteLine($"Zoom level {zoomLevel} not found in the TIFF.");
                return;
            }

            // Create output folder for the processed image
            Directory.CreateDirectory(outputFolder);

            // Check if the image is tiled
            bool isTiled = image.IsTiled();
            if (!isTiled)
            {
                Console.WriteLine("The TIFF image is not tiled.");
                return;
            }

            // Get tile width and height
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Allocate a buffer for one tile
            int tileSize = image.TileSize();
            byte[] buffer = new byte[tileSize];

            // Calculate pixel coordinates of the tile based on the quadkey and zoom level
            var (pixelX, pixelY) = TileToPixel(tileX, tileY, zoomLevel);

            // Calculate the geographic bounding box for the quadkey area
            var (minLon, minLat, maxLon, maxLat) = PixelToLatLonBounds(pixelX, pixelY, zoomLevel);

            Console.WriteLine($"Tile bounds: MinLon={minLon}, MinLat={minLat}, MaxLon={maxLon}, MaxLat={maxLat}");

            // Read the specific tile based on the pixel coordinates
            int tileIndex = image.ComputeTile(pixelX, pixelY, 0, 0);
            image.ReadTile(buffer, 0, pixelX, pixelY, 0, 0);

            // Save the tile as a 256x256 JPEG image
            SaveTileAsJpeg(buffer, tileWidth, tileHeight, tileX, tileY, zoomLevel, minLon, minLat, maxLon, maxLat, outputFolder);
        }
    }
    static bool NavigateToNearestZoomLevelByDimensions(Tiff image, int targetZoomLevel, out int foundZoomLevel)
    {
        int bestMatchDirectory = -1;
        int smallestDimensionDifference = int.MaxValue;
        foundZoomLevel = -1;

        // Calculate expected dimensions for the target zoom level
        int expectedTiles = (int)Math.Pow(2, targetZoomLevel);
        int expectedDimension = expectedTiles * 256; // Assuming each tile is 256x256 pixels

        // Start from the first directory and search for the best matching one
        image.SetDirectory(0);
        int directoryIndex = 0;

        do
        {
            // Get the image dimensions for the current directory
            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Calculate the difference between the expected dimension and the actual dimensions
            int dimensionDifference = Math.Abs(imageWidth - expectedDimension) + Math.Abs(imageHeight - expectedDimension);

            // If this directory's dimensions are closer to the expected dimensions, remember this directory
            if (dimensionDifference < smallestDimensionDifference)
            {
                smallestDimensionDifference = dimensionDifference;
                bestMatchDirectory = directoryIndex;
                foundZoomLevel = (int)Math.Log(imageWidth / 256, 2); // Estimate the zoom level from width (assuming square tiles)
            }

            directoryIndex++;

        } while (image.ReadDirectory());

        // If a best match directory was found, set the TIFF reader to that directory
        if (bestMatchDirectory != -1)
        {
            image.SetDirectory((short)bestMatchDirectory);
            return true;
        }

        return false;
    }
    // Convert a quadkey to tile coordinates (X, Y) and zoom level
    static (int tileX, int tileY, int zoom) QuadKeyToTile(string quadkey)
    {
        int tileX = 0;
        int tileY = 0;
        int zoom = quadkey.Length;

        for (int i = 0; i < zoom; i++)
        {
            int mask = 1 << (zoom - i - 1);
            switch (quadkey[i])
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
                    throw new ArgumentException("Invalid quadkey.");
            }
        }

        return (tileX, tileY, zoom);
    }

    // Convert tile coordinates to pixel coordinates at the given zoom level (Web Mercator projection)
    static (int pixelX, int pixelY) TileToPixel(int tileX, int tileY, int zoom)
    {
        int pixelX = tileX * 256;
        int pixelY = tileY * 256;
        return (pixelX, pixelY);
    }

    // Convert pixel coordinates to latitude/longitude bounds using Web Mercator
    static (double minLon, double minLat, double maxLon, double maxLat) PixelToLatLonBounds(int pixelX, int pixelY, int zoom)
    {
        double mapSize = 256 * Math.Pow(2, zoom);

        double minLon = (pixelX / mapSize) * 360.0 - 180.0;
        double maxLat = 90.0 - 360.0 * Math.Atan(Math.Exp((1 - 2 * pixelY / mapSize) * Math.PI)) / Math.PI;
        double maxLon = ((pixelX + 256) / mapSize) * 360.0 - 180.0;
        double minLat = 90.0 - 360.0 * Math.Atan(Math.Exp((1 - 2 * (pixelY + 256) / mapSize) * Math.PI)) / Math.PI;

        return (minLon, minLat, maxLon, maxLat);
    }

    // Save tile as a 256x256 JPEG image with bounding box information
    static void SaveTileAsJpeg(byte[] buffer, int tileWidth, int tileHeight, int col, int row, int zoomLevel, double minLon, double minLat, double maxLon, double maxLat, string outputFolder)
    {
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Copy the buffer data into the bitmap's pixel buffer
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            // Resize the image to 256x256
            using (Bitmap resizedTile = new Bitmap(bitmap, new Size(256, 256)))
            {
                // Construct the file name with tile coordinates, zoom level, and bounding box
                string fileName = Path.Combine(outputFolder, $"tile_{zoomLevel}_{col}_{row}_bounds_{minLon:F6}_{minLat:F6}_{maxLon:F6}_{maxLat:F6}_256x256.jpeg");

                // Save the resized bitmap as a JPEG file
                resizedTile.Save(fileName, ImageFormat.Jpeg);
            }
        }
    }
  
    public static (double lat, double lon) PixelXYToLatLong(int pixelX, int pixelY, int level)
    {
        double mapSize = 256 << level;
        double x = (pixelX / mapSize) - 0.5;
        double y = 0.5 - (pixelY / mapSize);

        double lat = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
        double lon = 360 * x;

        return (lat, lon);
    }

    public static (double north, double south, double east, double west) TileXYToBoundingBox(int tileX, int tileY, int level)
    {
        (int pixelX, int pixelY) topLeft = (tileX * 256, tileY * 256);
        (int pixelX, int pixelY) bottomRight = ((tileX + 1) * 256 - 1, (tileY + 1) * 256 - 1);

        var (north, west) = PixelXYToLatLong(topLeft.pixelX, topLeft.pixelY, level);
        var (south, east) = PixelXYToLatLong(bottomRight.pixelX, bottomRight.pixelY, level);

        return (north, south, east, west);
    }


}

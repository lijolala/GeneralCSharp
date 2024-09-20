using System;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main(string[] args)
    {
        //// Define the bounding box (lat/lon)
        //double minLon = -120.0; // Min Longitude (left)
        //double maxLon = -119.0; // Max Longitude (right)
        //double minLat = 35.0;   // Min Latitude (bottom)
        //double maxLat = 36.0;   // Max Latitude (top)

        // Path to your GeoTIFF file
        string quadKey = "1202102332";
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;
        var (tileX, tileY, level) = QuadKeyToTileXY(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileXYToBoundingBox(tileX, tileY, level);
    //    GetCorrectZoomDirectory(geoTiffFilePath, zoomLevel);
        // Open the TIFF file
        using (Tiff tiff = Tiff.Open(tiffFilePath, "r"))
        {
            if (tiff == null)
            {
                Console.WriteLine("Could not open file.");
                return;
            }
            // Loop through all directories in the GeoTIFF
            short directoryCount = tiff.NumberOfDirectories();
            tiff.SetDirectory(level <= directoryCount ? level : directoryCount);
            // Get the image width and height
            int imageWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            double[] pixelScale = { 0.005000000000002559689, -0.004999999999999006108 };
            double[] tiePoint = { -180.09, 90.048 };

            // Convert bounding box coordinates to pixel coordinates
            var (xMin, yMin) = GeoToPixel(minLat, minLon, tiePoint, pixelScale);
            var (pixelXMax, pixelYMax) = GeoToPixel(maxLat, maxLon, tiePoint, pixelScale);
           int boxWidth = Math.Abs(pixelXMax - xMin);
           int boxHeight = Math.Abs(pixelYMax - yMin);
            pixelXMax = Math.Min(imageWidth, pixelXMax);
            pixelYMax = Math.Min(imageHeight, pixelYMax);

            // Set the region of interest (example pixel coordinates)
            //int xMin = 100;  // Replace with actual xMin pixel coordinate
            //int yMin = 100;  // Replace with actual yMin pixel coordinate
            //int boxWidth = 200; // Width of the region you want to read
            //int boxHeight = 200; // Height of the region you want to read

            // Make sure the requested region is within the image bounds
            if (xMin + boxWidth > imageWidth || yMin + boxHeight > imageHeight)
            {
                Console.WriteLine("Requested region is outside image bounds.");
                return;
            }

            // Allocate buffer for reading the region
            byte[] buffer = new byte[boxWidth * boxHeight];
            int nooftiles = tiff.GetField(TiffTag.TILEBYTECOUNTS).Length;

            //// Iterate over the region and read it row by row
            //for (int row = 0; row < boxHeight; row++)
            //{
            //    int rowIndex = yMin + row;
            //    byte[] scanline = new byte[boxWidth * 4];  // 4 bytes per pixel (RGBA)

            //    // Read the scanline for the row within the bounding box
            //    tiff.ReadEncodedStrip(rowIndex, scanline, 0, scanline.Length);

            //    // Copy scanline to the buffer (this is an example, adapt to your needs)
            //    Buffer.BlockCopy(scanline, xMin * 4, buffer, row * boxWidth * 4, boxWidth * 4);
            //}
            for (int i = 0; i < nooftiles; i++)
            {
                int size = tiff.ReadEncodedTile(i, buffer, 0, boxWidth * boxHeight);
                float[,] data = new float[boxWidth, boxHeight];
                Buffer.BlockCopy(buffer, 0, data, 0, size); // Convert byte array to x,y array of floats (height data)
                // Do whatever you want with the height data (calculate hillshade images etc.)
            }

            // Process or save the buffer as needed (you can now work with the pixel data)
            Console.WriteLine("Successfully extracted region from the TIFF.");
        }
    }

    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, short level) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        short level =(short) quadKey.Length;

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

    public static (int pixelX, int pixelY) GeoToPixel(double lat, double lon, double[] tiePoint, double[] pixelScale)
    {
        double pixelX = (lon - tiePoint[0]) / pixelScale[0];
        double pixelY = (lat - tiePoint[1]) / -pixelScale[1]; // Note: y is flipped in many coordinate systems

        return ((int)Math.Round(pixelX), (int)Math.Round(pixelY));
    }
}

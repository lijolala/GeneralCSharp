using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static void Main(string[] args)
    {
        // Path to your TIFF file
        string tiffFile = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png";

        // Define the bounding box (lat/lon)
        double minLon = 13.35; // Min Longitude (left)
        double maxLon = 13.71; // Max Longitude (right)
        double minLat = 52.69;   // Min Latitude (bottom)
        double maxLat = 52.48;   // Max Latitude (top)

        int tileWidth = 256;     // Example tile width
        int tileHeight = 256;    // Example tile height
        // Open the TIFF file
        using (Tiff tiff = Tiff.Open(tiffFile, "r"))
        {
            if (tiff == null)
            {
                Console.WriteLine("Could not open file.");
                return;
            }

            // Get image dimensions
            int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Get tile dimensions
            int tileWidthFromTiff = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeightFromTiff = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Assuming the geoTransform array is already defined
            double[] geoTransform = new double[] { -180.00000000000006, 0.00500000000000256, 0, -90.000000000000028, 0, -0.0049999999999990061 };

            // Replace the following line with actual geoTransform retrieval
            // Example: { topLeftX, pixelWidth, 0, topLeftY, 0, pixelHeight }
            //geoTransform[0] = /* top-left longitude */;
            //geoTransform[1] = /* pixel width */;
            //geoTransform[3] = /* top-left latitude */;
            //geoTransform[5] = /* pixel height */; // Typically negative

            // Convert bounding box to pixel coordinates
            int xMin = (int)((minLon - geoTransform[0]) / geoTransform[1]);
            int xMax = (int)((maxLon - geoTransform[0]) / geoTransform[1]);
            int yMin = (int)((minLat - geoTransform[3]) / geoTransform[5]);
            int yMax = (int)((maxLat - geoTransform[3]) / geoTransform[5]);

            // Adjust bounds to fit image dimensions
            xMin = Clamp(xMin, 0, width - 1);
            xMax = Clamp(xMax, 0, width - 1);
            yMin = Clamp(yMin, 0, height - 1);
            yMax = Clamp(yMax, 0, height - 1);

            // Create bitmap for the output image
            // Create a bitmap to store the extracted part of the image
            //int boxWidth = xMax - xMin + 1;
            //int boxHeight = yMax - yMin + 1;
            int boxWidth = tileWidth;
            int boxHeight = tileHeight;

            Bitmap bmp = new Bitmap(boxWidth, boxHeight, PixelFormat.Format24bppRgb);

            // Loop through tiles and extract relevant data
            byte[] buffer = new byte[tileWidthFromTiff * tileHeightFromTiff * sizeof(float)];
            int noOfTiles = tiff.NumberOfTiles();

            // Read the relevant pixel data
            // byte[] buffer = new byte[(xMax - xMin + 1) * (yMax - yMin + 1) * sizeof(float)];

            for (int tileIndex = 0; tileIndex < noOfTiles; tileIndex++)
            {
                // Read encoded tile data
                int size = tiff.ReadEncodedTile(tileIndex, buffer, 0, tileWidthFromTiff * tileHeightFromTiff * sizeof(float));

                // Convert byte buffer to 2D array of float values
                float[,] data = new float[tileWidthFromTiff, tileHeightFromTiff];
                Buffer.BlockCopy(buffer, 0, data, 0, size);

                // Calculate the tile's pixel coordinates within the image
                int tileX = (tileIndex % (width / tileWidthFromTiff)) * tileWidthFromTiff;
                int tileY = (tileIndex / (width / tileWidthFromTiff)) * tileHeightFromTiff;

                // Check if the tile intersects with the bounding box
                if (tileX + tileWidthFromTiff < xMin || tileX > xMax || tileY + tileHeightFromTiff < yMin || tileY > yMax)
                {
                    continue; // Skip if outside bounding box
                }

                // Loop through each pixel in the tile and copy the relevant pixels to the output bitmap
                for (int y = 0; y < tileHeightFromTiff; y++)
                {
                    for (int x = 0; x < tileWidthFromTiff; x++)
                    {
                        int imageX = tileX + x;
                        int imageY = tileY + y;

                        // Skip pixels outside the bounding box
                        if (imageX < xMin || imageX > xMax || imageY < yMin || imageY > yMax)
                        {
                            continue;
                        }

                        // Normalize and convert to grayscale (adjust based on your data)
                        float value = data[x, y];
                        int grayValue = (int)(255 * value);  // Normalize value between 0 and 255
                        grayValue = Clamp(grayValue, 0, 255);

                        // Set the pixel in the bitmap (subtracting xMin, yMin to fit into new bitmap)
                        Color color = Color.FromArgb(grayValue, grayValue, grayValue);
                        bmp.SetPixel(imageX - xMin, imageY - yMin, color);
                    }
                }
            }

            // Save the bitmap to disk
            bmp.Save(outputFilePath, ImageFormat.Png);
            Console.WriteLine($"Image saved as {outputFilePath}");

            // Clean up
            bmp.Dispose();
        }
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

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
        string quadKey = "1202102332";
        //string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;
        int tileWidth = 256;   // Example tile width
        int tileHeight = 256;  // Example tile height

        // Open the TIFF file
        using (Tiff tiff = Tiff.Open(tiffFile, "r"))
        {
            if (tiff == null)
            {
                Console.WriteLine("Could not open file.");
                return;
            }

            // Determine the number of tiles
            int noOfTiles = tiff.NumberOfTiles();
            byte[] buffer = new byte[tileWidth * tileHeight * sizeof(float)];

            for (int i = 0; i < noOfTiles; i++)
            {
                // Read encoded tile data
                int size = tiff.ReadEncodedTile(i, buffer, 0, tileWidth * tileHeight * sizeof(float));

                // Convert the byte buffer into a 2D float array
                float[,] data = new float[tileWidth, tileHeight];
                Buffer.BlockCopy(buffer, 0, data, 0, size); // Convert byte array to 2D float array

                // Normalize and convert to image
                Bitmap bmp = new Bitmap(tileWidth, tileHeight, PixelFormat.Format24bppRgb);

                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        // Normalize the float data (assuming height data, or adjust based on your needs)
                        float value = data[x, y];
                        int grayValue = (int)(255 * value);  // Normalize the value between 0 and 255

                        grayValue = Clamp(grayValue, 0, 255);  // Ensure within valid range

                        // Set the pixel as grayscale (R=G=B)
                        Color color = Color.FromArgb(grayValue, grayValue, grayValue);
                        bmp.SetPixel(x, y, color);
                    }
                }

                // Save the bitmap to disk
                string outputFile = $"tile_{i}.png";
                bmp.Save(outputFile, ImageFormat.Png);
                Console.WriteLine($"Saved tile {i} as {outputFile}");

                // Dispose of the bitmap
                bmp.Dispose();
            }
        }
    }
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

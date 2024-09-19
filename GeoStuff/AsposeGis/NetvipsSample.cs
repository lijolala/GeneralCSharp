using System;
using Aspose.Gis;
using Aspose.Gis.Raster;
using Aspose.Imaging;

class Program
{
    static void Main(string[] args)
    {
        // Path to the raster file (e.g., GeoTIFF)
        string rasterFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; ;

        // Open the raster file
        using (RasterImage rasterImage = (RasterImage)RasterImage.Load(rasterFilePath))
        {
            // Get the raster image dimensions
            int width = rasterImage.Width;
            int height = rasterImage.Height;

            Console.WriteLine($"Raster Width: {width}, Height: {height}");

            // Loop through the pixels or read specific pixel values
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get pixel values (can be in different formats, e.g., RGB or Grayscale)
                    var pixelValue = rasterImage.GetPixel(x, y);

                    // Print pixel values for demonstration
                    Console.WriteLine($"Pixel at ({x}, {y}): {pixelValue}");
                }
            }
        }
    }
}

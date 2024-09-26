using Aspose.Gis;
using Aspose.Gis.Raster;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Aspose.Gis.Formats.GeoTiff;

namespace GeoTiffLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Path to your GeoTIFF file
            string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war.tif";
            string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;

            // Open the GeoTIFF as a Raster Layer using the GeoTiff driver
            using (RasterLayer layer = Drivers.GeoTiff.OpenLayer(tiffFilePath))
            {
                // Get basic properties of the GeoTIFF
                Console.WriteLine($"Raster Size: {layer.Width} x {layer.Height}");
                Console.WriteLine($"Spatial Reference: {layer.SpatialReferenceSystem}");
                Console.WriteLine($"Bands: {layer.BandCount}");

                // Example: Create a 256x256 image from the center of the GeoTIFF
                int tileSize = 256;
                int centerX = layer.Width / 2;
                int centerY = layer.Height / 2;

                using (Bitmap bmp = new Bitmap(tileSize, tileSize, PixelFormat.Format24bppRgb))
                {
                    for (int y = 0; y < tileSize; y++)
                    {
                        for (int x = 0; x < tileSize; x++)
                        {
                            // Get the corresponding coordinates in the full GeoTIFF
                            int globalX = centerX - tileSize / 2 + x;
                            int globalY = centerY - tileSize / 2 + y;

                            // Ensure coordinates are within bounds
                            if (globalX < 0 || globalY < 0 || globalX >= layer.Width || globalY >= layer.Height)
                                continue;

                            // Get pixel values (assuming 3 bands: Red, Green, Blue)
                            var pixel = layer.GetValues(globalX, globalY);

                            // Set the pixel in the bitmap
                            bmp.SetPixel(x, y, Color.FromArgb((int)pixel[0], (int)pixel[1], (int)pixel[2]));
                        }
                    }


                    // Save the 256x256 cropped area as a new image
                    bmp.Save(outputFilePath);
                    Console.WriteLine("Cropped GeoTIFF saved as a 256x256 image.");
                }
            }
        }
    }
}

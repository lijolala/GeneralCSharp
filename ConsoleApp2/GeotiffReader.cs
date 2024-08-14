using System;
using OSGeo.GDAL;

namespace COGReader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize GDAL
            Environment.SetEnvironmentVariable("GDAL_DATA", @"C:\GDAL3.9.1\bin\gdal-data");
            Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", @"C:\GDAL3.9.1\bin\gdal\plugins");
            Gdal.AllRegister();

            // Path to the COG file
            string filePath = @"D:\Everbridge\Story\VCC-6740-IHS\TCI.tif";

            // Open the dataset
            Dataset dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset == null)
            {
                Console.WriteLine("Failed to open COG file.");
                return;
            }

            // Get raster band (e.g., the first band)
            Band band = dataset.GetRasterBand(1);

            // Get the size of the raster
            int width = band.XSize;
            int height = band.YSize;

            // Print raster dimensions
            Console.WriteLine($"Width: {width}, Height: {height}");

            // Read raster data into a buffer
            float[] buffer = new float[width * height];
            band.ReadRaster(0, 0, width, height, buffer, width, height, 0, 0);

            // Print some pixel values
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Pixel value at {i}: {buffer[i]}");
            }

            // Get geotransform information
            double[] geoTransform = new double[6];
            dataset.GetGeoTransform(geoTransform);
            Console.WriteLine($"Origin: ({geoTransform[0]}, {geoTransform[3]})");
            Console.WriteLine($"Pixel Size: ({geoTransform[1]}, {geoTransform[5]})");

            // Get projection
            string projection = dataset.GetProjection();
            Console.WriteLine($"Projection: {projection}");

            // Close the dataset
            dataset.Dispose();
        }
    }
}

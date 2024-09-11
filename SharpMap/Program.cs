using System;
using DotSpatial.Data;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\GeoTiff\sample.tif"; // Your GeoTIFF file path

        // Open the GeoTIFF
        IRaster raster = Raster.OpenFile(filePath);
        if (raster != null)
        {
            Console.WriteLine($"Bounds: {raster.Bounds}");
            Console.WriteLine($"Num Rows: {raster.NumRows}, Num Columns: {raster.NumColumns}");
            Console.WriteLine($"Cell Size: {raster.CellWidth} x {raster.CellHeight}");

            // Reading a pixel value at row 0, column 0
            double value = raster.Value[0, 0];
            Console.WriteLine($"Value at (0,0): {value}");
        }
        else
        {
            Console.WriteLine("Failed to open GeoTIFF");
        }
    }
}
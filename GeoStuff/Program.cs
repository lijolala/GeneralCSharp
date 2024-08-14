using System;

using DotSpatial.Data;
using DotSpatial.Projections;

namespace GeoTiffReader
{
    class Program
    {
        static void Main(string[] args)
        {
            // Path to the GeoTIFF file
            string filePath = @"path\to\your\file.tif";

            // Read the raster file
            IRaster raster = Raster.OpenFile(filePath);

            // Display some basic information about the raster
            Console.WriteLine($"Number of Rows: {raster.NumRows}");
            Console.WriteLine($"Number of Columns: {raster.NumColumns}");
            Console.WriteLine($"Cell Width: {raster.CellWidth}");
            Console.WriteLine($"Cell Height: {raster.CellHeight}");
            Console.WriteLine($"Bounds: {raster.Bounds}");

            // You can access raster values by specifying row and column indices
            double value = raster.Value[0, 0]; // Value at the first row and first column
            Console.WriteLine($"Value at (0,0): {value}");

            // If you want to project the raster to a different coordinate system:
            ProjectionInfo targetProjection = KnownCoordinateSystems.Projected.World.WebMercator;
            raster = Raster.ReprojectRaster(raster, targetProjection);

            // Save the reprojected raster if necessary
            string outputFilePath = @"path\to\your\outputfile.tif";
            raster.SaveAs(outputFilePath);
        }
    }
}
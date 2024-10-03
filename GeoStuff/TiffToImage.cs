using System;
using System.Security.AccessControl;
using NetVips;

class Program
{
    static void Main(string[] args)
    {
        string inputGeoTiffPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputPngPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\WarImage.png";

        // Load the GeoTIFF image
        var image = Image.NewFromFile(inputGeoTiffPath, access: Enums.Access.Sequential);

        // Get original image dimensions
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        // Set the target size (256 pixels)
        int targetSize = 1200;

        // Calculate scaling factors
        // Calculate the scaling factor while maintaining aspect ratio
        double scale = Math.Min((double)targetSize / originalWidth, (double)targetSize / originalHeight);

        // Resize the image
        var resizedImage = image.Resize(scale);

        // Save the resized image as a PNG without loss of data
        resizedImage.WriteToFile(outputPngPath);


        // Save the resized image as PNG
        resizedImage.WriteToFile(outputPngPath);

        Console.WriteLine($"Converted {inputGeoTiffPath} to {outputPngPath} with resolution of 256 pixels.");
    }
}
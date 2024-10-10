using System;

using NetVips;

class Program
{
    static void Main(string[] args)
    {
        // Load the GeoTIFF image
        string inputFile = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputPngPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\WarImage.png";
        Image image = Image.NewFromFile(inputFile);

        // Check if image is loaded correctly
        Console.WriteLine($"Width: {image.Width}, Height: {image.Height}, Bands: {image.Bands}");

        if (image.Width > 0 && image.Height > 0)
        {
            double scaleFactor = 0.5;  // Change this to any desired scale factor
            image = image.Resize(scaleFactor);
            // If image has more than 3 bands, extract RGB channels
            if (image.Bands > 3)
            {
                // Extract the first three bands (assuming they are Red, Green, and Blue)
                Image redBand = image.ExtractBand(0);  // Red
                Image greenBand = image.ExtractBand(1);  // Green
                Image blueBand = image.ExtractBand(2);  // Blue

                // Join the three bands into one image
                image = Image.Arrayjoin(new Image[] { redBand, greenBand, blueBand });
            }
            double maxVal = image.Max();

            // Normalize the pixel values to the range 0-255 using the max value
            image = image.Linear(new double[] { 255 / maxVal, 255 / maxVal, 255 / maxVal }, new double[] { 0, 0, 0 });


            // Set DPI (dots per inch)
            double dpi = 300;
            double pixelsPerMm = dpi / 25.4;

            // Prepare options for PNG export, including DPI settings
            var exportOptions = new VOption
            {
                {"resolution-x", pixelsPerMm}, // Horizontal resolution in pixels per mm
                {"resolution-y", pixelsPerMm}  // Vertical resolution in pixels per mm
            };

           // // Export the image to PNG with custom resolution
          //  string outputFile = "output.png";
            image.WriteToFile(outputPngPath);

            //Console.WriteLine($"Image successfully written to {outputFile} with DPI settings.");
        }
        else
        {
            Console.WriteLine("Failed to load the image.");
        }
    }
}
using System;

using NetVips;

class Program
{
    static void Main(string[] args)
    {
        string inputFile = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputPngPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\WarImage.png";
        Image image = Image.NewFromFile(inputFile);

        Console.WriteLine($"Width: {image.Width}, Height: {image.Height}, Bands: {image.Bands}");

        if (image.Width > 0 && image.Height > 0)
        {
            double scaleFactor = 0.5;  // Scale factor can be adjusted
            image = image.Resize(scaleFactor);

            // Check min/max values for each band
            for (int i = 0; i < image.Bands; i++)
            {
                Image band = image.ExtractBand(i);
                Console.WriteLine($"Band {i}: Min = {band.Min()}, Max = {band.Max()}");
            }

            // If image has more than 3 bands, extract RGB channels
            if (image.Bands > 3)
            {
                Image redBand = image.ExtractBand(0);  // Assuming band 0 is Red
                Image greenBand = image.ExtractBand(1);  // Assuming band 1 is Green
                Image blueBand = image.ExtractBand(2);  // Assuming band 2 is Blue

                // Normalize each band individually
                // Normalize each band individually
                redBand = redBand.Linear(new double[] { 255 / redBand.Max() }, new double[] { 0 });
                greenBand = greenBand.Linear(new double[] { 255 / greenBand.Max() }, new double[] { 0 });
                blueBand = blueBand.Linear(new double[] { 255 / blueBand.Max() }, new double[] { 0 });


                // Join the bands into a single image
                image = Image.Arrayjoin(new Image[] { redBand, greenBand, blueBand });
            }

            // Optional gamma correction
            image = image.Gamma(1 / 2.2);

            // Save the image as PNG
            image.WriteToFile(outputPngPath);
            Console.WriteLine($"Image successfully written to {outputPngPath}");
        }
        else
        {
            Console.WriteLine("Failed to load the image.");
        }
    }
}

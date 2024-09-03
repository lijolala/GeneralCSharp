using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using BitMiracle.LibTiff.Classic;

class Program
{
    static async Task Main(string[] args)
    {
        string url = "http://127.0.0.1:5500/cogs/war_2023-08-19.tif"; // Replace with your actual URL
        string outputFilePath = "downloaded_geotiff.tif";

        // Step 1: Download the GeoTIFF file
        using (HttpClient client = new HttpClient())
        {
            byte[] fileBytes = await client.GetByteArrayAsync(url);
            File.WriteAllBytes(outputFilePath, fileBytes);
        }

        Console.WriteLine("GeoTIFF file downloaded successfully.");

        // Step 2: Open the file with libtiff and process it in strips
        OpenGeoTiff(outputFilePath);
    }

    static void OpenGeoTiff(string filePath)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the GeoTIFF file.");
                return;
            }

            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            Console.WriteLine($"Width: {width}, Height: {height}");

            // Process the image in strips
            int strips = image.NumberOfStrips();
            Console.WriteLine($"Number of strips: {strips}");

            for (int strip = 0; strip < strips; strip++)
            {
                int stripSize = image.StripSize();
                byte[] stripData = new byte[stripSize];

                int bytesRead = image.ReadEncodedStrip(strip, stripData, 0, stripSize);
                if (bytesRead <= 0)
                {
                    Console.WriteLine($"Failed to read strip {strip}");
                    continue;
                }

                // Process the strip data here (e.g., save, analyze, etc.)
                Console.WriteLine($"Processed strip {strip + 1}/{strips}, Size: {stripSize} bytes");
            }

            Console.WriteLine("GeoTIFF file processed successfully.");
        }
    }
}

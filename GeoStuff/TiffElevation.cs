using BitMiracle.LibTiff.Classic;

using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";

        using (Tiff image = Tiff.Open(tiffFilePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int sampleFormat = image.GetField(TiffTag.SAMPLEFORMAT)[0].ToInt();

            if (samplesPerPixel != 1)
            {
                Console.WriteLine("This example assumes a single-band raster (elevation data).");
                return;
            }

            // Create a buffer to store a row of pixel data
            int scanlineSize = image.ScanlineSize();
            byte[] buffer = new byte[scanlineSize];

            // Determine the data type based on bits per sample and sample format
            Func<byte[], int, double> convertSample;
            if (sampleFormat == (int)SampleFormat.IEEEFP && bitsPerSample == 32)
            {
                // 32-bit floating point data
                convertSample = (data, index) => BitConverter.ToSingle(data, index);
            }
            else if (sampleFormat == (int)SampleFormat.INT && bitsPerSample == 16)
            {
                // 16-bit signed integer data
                convertSample = (data, index) => BitConverter.ToInt16(data, index);
            }
            else if (sampleFormat == (int)SampleFormat.UINT && bitsPerSample == 16)
            {
                // 16-bit unsigned integer data
                convertSample = (data, index) => BitConverter.ToUInt16(data, index);
            }
            else
            {
                Console.WriteLine("Unsupported data type or bit depth.");
                return;
            }

            // Iterate through each row and extract elevation data
            for (int y = 0; y < imageHeight; y++)
            {
                image.ReadScanline(buffer, y);

                for (int x = 0; x < imageWidth; x++)
                {
                    // Calculate the index based on bits per sample
                    int index = x * (bitsPerSample / 8);
                    double elevation = convertSample(buffer, index);

                    // Do something with the elevation data
                    Console.WriteLine($"Elevation at ({x}, {y}) = {elevation}");
                }
            }
        }
    }
}

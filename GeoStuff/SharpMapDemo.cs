using System;
using System.Drawing;
using System.Drawing.Imaging;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main()
    {
        // Path to your GeoTIFF file
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; ;

        try
        {
            // Open the TIFF file
            using (Tiff tiff = Tiff.Open(filePath, "r"))
            {
                // Get image dimensions
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int samplesPerPixel = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
                int bitsPerSample = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

                if (samplesPerPixel != 1 || bitsPerSample != 8)
                {
                    throw new NotSupportedException("Only 8-bit grayscale images are supported.");
                }

                // Create a Bitmap with the same dimensions
                using (Bitmap bitmap = new Bitmap(800, 400, PixelFormat.Format8bppIndexed))
                {
                    // Set the grayscale palette
                    ColorPalette palette = bitmap.Palette;
                    for (int i = 0; i < 256; i++)
                    {
                        palette.Entries[i] = Color.FromArgb(i, i, i);
                    }
                    bitmap.Palette = palette;

                    // Create a byte array to hold one row of pixels
                    int bytesPerRow = width; // 8-bit image, 1 byte per pixel
                    byte[] buffer = new byte[bytesPerRow];

                    // Read each row of the image
                    for (int row = 0; row < height; row++)
                    {
                        if (tiff.ReadScanline(buffer, row) == null)
                        {
                            throw new InvalidOperationException("Failed to read scanline");
                        }

                        // Convert the buffer data to the Bitmap
                        for (int col = 0; col < width; col++)
                        {
                            byte pixelValue = buffer[col];
                            bitmap.SetPixel(col, row, palette.Entries[pixelValue]);
                        }
                    }

                    // Save the Bitmap as an image file (e.g., PNG)
                    bitmap.Save(@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\out.png", ImageFormat.Png);
                }

                Console.WriteLine("GeoTIFF image saved successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

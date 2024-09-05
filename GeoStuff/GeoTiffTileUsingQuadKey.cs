using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; // Replace with your GeoTIFF file path
        string quadkey = "02310"; // Replace with your quadkey
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1"; // Replace with your output folder path

        var result = DecodeQuadkey(quadkey);
        int zoomLevel = result.Item1;
        int tileX = result.Item2;
        int tileY = result.Item3;

        Console.WriteLine($"Zoom Level: {zoomLevel}, Tile X: {tileX}, Tile Y: {tileY}");

        try
        {
            SaveTileFromGeoTiff(filePath, zoomLevel, tileX, tileY, outputFolder);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static Tuple<int, int, int> DecodeQuadkey(string quadkey)
    {
        int zoomLevel = quadkey.Length;
        int tileX = 0;
        int tileY = 0;

        for (int i = zoomLevel - 1; i >= 0; i--)
        {
            char digit = quadkey[i];
            int mask = 1 << i;
            switch (digit)
            {
                case '0':
                    break;
                case '1':
                    tileX |= mask;
                    break;
                case '2':
                    tileY |= mask;
                    break;
                case '3':
                    tileX |= mask;
                    tileY |= mask;
                    break;
            }
        }

        return Tuple.Create(zoomLevel, tileX, tileY);
    }

    static void SaveTileFromGeoTiff(string filePath, int zoomLevel, int tileX, int tileY, string outputFolder)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                throw new InvalidOperationException("Could not open the GeoTIFF file.");
            }

            // Get tile dimensions
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int bytesPerSample = GetBytesPerSample(image);
            int bytesPerPixel = bytesPerSample * samplesPerPixel;

            // Calculate the pixel buffer size based on the image’s sample format
            int bytesPerTile = tileWidth * tileHeight * bytesPerPixel;

            // Allocate buffer for tile data
            byte[] raster = new byte[bytesPerTile];

            // Calculate the tile index
            int numTilesX = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt() / tileWidth;
            int tileIndex = tileY * numTilesX + tileX;

            // Read the tile data
            if (image.ReadEncodedTile(tileIndex, raster, 0, raster.Length) == -1)
            {
                throw new InvalidOperationException($"Could not read tile {tileIndex}.");
            }

            // Convert the byte array to a bitmap using SetPixel
            Bitmap bitmap = ConvertToBitmapWithSetPixel(raster, tileWidth, tileHeight, samplesPerPixel, bitsPerSample);

            // Save the bitmap as JPEG
            string outputFilePath = Path.Combine(outputFolder, $"tile_{tileX}_{tileY}.jpg");
            bitmap.Save(outputFilePath, ImageFormat.Jpeg);

            Console.WriteLine($"Tile saved to {outputFilePath}.");
        }
    }

    static int GetBytesPerSample(Tiff image)
    {
        var bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE);
        if (bitsPerSample == null)
        {
            throw new InvalidOperationException("Unable to determine bits per sample.");
        }
        int bits = bitsPerSample[0].ToInt();
        return (bits + 7) / 8; // Convert bits to bytes
    }

    static Bitmap ConvertToBitmapWithSetPixel(byte[] raster, int width, int height, int samplesPerPixel, int bitsPerSample)
    {
        Bitmap bitmap = new Bitmap(width, height);

        int bytesPerPixel = (bitsPerSample * samplesPerPixel + 7) / 8;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = (y * width + x) * bytesPerPixel;

                // Assuming 8-bit grayscale or 24-bit RGB
                if (samplesPerPixel == 1)
                {
                    // Grayscale
                    int intensity = raster[pixelIndex];
                    Color color = Color.FromArgb(intensity, intensity, intensity);
                    bitmap.SetPixel(x, y, color);
                }
                else if (samplesPerPixel == 3)
                {
                    // RGB
                    int red = raster[pixelIndex];
                    int green = raster[pixelIndex + 1];
                    int blue = raster[pixelIndex + 2];
                    Color color = Color.FromArgb(red, green, blue);
                    bitmap.SetPixel(x, y, color);
                }
                else if (samplesPerPixel == 4)
                {
                    // RGBA
                    int red = raster[pixelIndex];
                    int green = raster[pixelIndex + 1];
                    int blue = raster[pixelIndex + 2];
                    int alpha = raster[pixelIndex + 3];
                    Color color = Color.FromArgb(alpha, red, green, blue);
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        return bitmap;
    }
}

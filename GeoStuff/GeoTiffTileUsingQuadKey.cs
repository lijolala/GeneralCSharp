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

            // Convert the byte array to a bitmap
            Bitmap bitmap = ConvertToBitmap(raster, tileWidth, tileHeight, samplesPerPixel, bitsPerSample);

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

    static Bitmap ConvertToBitmap(byte[] raster, int width, int height, int samplesPerPixel, int bitsPerSample)
    {
        int bytesPerPixel = (bitsPerSample * samplesPerPixel + 7) / 8;

        PixelFormat pixelFormat = PixelFormat.Undefined;
        if (samplesPerPixel == 1)
        {
            pixelFormat = PixelFormat.Format8bppIndexed;
        }
        else if (samplesPerPixel == 3)
        {
            pixelFormat = PixelFormat.Format24bppRgb;
        }
        else if (samplesPerPixel == 4)
        {
            pixelFormat = PixelFormat.Format32bppArgb;
        }

        Bitmap bitmap = new Bitmap(width, height, pixelFormat);
        BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);

        // Ensure the raster length matches the bitmap data size
        if (raster.Length != width * height * bytesPerPixel)
        {
            throw new InvalidOperationException("Raster length does not match expected bitmap data size.");
        }

        // Copy the buffer data into the bitmap's pixel buffer
        System.Runtime.InteropServices.Marshal.Copy(raster, 0, bmpData.Scan0, raster.Length);

        bitmap.UnlockBits(bmpData);

        // Set grayscale palette for 8bpp
        if (pixelFormat == PixelFormat.Format8bppIndexed)
        {
            ColorPalette palette = bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            bitmap.Palette = palette;
        }

        return bitmap;
    }
}

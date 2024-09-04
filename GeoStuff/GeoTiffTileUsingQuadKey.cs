using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; ; // Replace with your GeoTIFF file path
        string quadkey = "02310"; // Replace with your quadkey
        string outputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1"; // Replace with your output folder path
        float scaleFactor = 0.1f; // Scale down by 10% (0.1f)

        (int zoomLevel, int tileX, int tileY) = DecodeQuadkey(quadkey);

        Console.WriteLine($"Zoom Level: {zoomLevel}, Tile X: {tileX}, Tile Y: {tileY}");

        ResizeAndSaveTileFromGeoTiff(filePath, zoomLevel, tileX, tileY, outputFolder, scaleFactor);
    }

    static (int zoomLevel, int tileX, int tileY) DecodeQuadkey(string quadkey)
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
                    // No change
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

        return (zoomLevel, tileX, tileY);
    }

    static void ResizeAndSaveTileFromGeoTiff(string filePath, int zoomLevel, int tileX, int tileY, string outputFolder, float scaleFactor)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the GeoTIFF file.");
                return;
            }

            // Get tile dimensions
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Calculate the pixel buffer size based on the image’s sample format
            int bytesPerTile = tileWidth * tileHeight * GetBytesPerSample(image);

            // Allocate buffer for tile data
            byte[] raster = new byte[bytesPerTile];

            // Calculate the tile index
            int numTilesX = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt() / tileWidth;
            int tileIndex = tileY * numTilesX + tileX;

            // Read the tile data
            if (image.ReadRawTile(tileIndex, raster, 0, raster.Length) == null)
            {
                Console.WriteLine($"Could not read tile {tileIndex}.");
                return;
            }

            // Convert the byte array to a bitmap
            //Bitmap originalBitmap = ConvertToBitmap(raster, (int)(tileWidth * scaleFactor), (int)(tileHeight * scaleFactor), GetBytesPerSample(image));
            Bitmap originalBitmap = ConvertToBitmap(raster, (int)(tileWidth ), (int)(tileHeight ), GetBytesPerSample(image));

            // Save the resized bitmap
            string outputFilePath = Path.Combine(outputFolder, $"tile_{tileX}_{tileY}.png");
            originalBitmap.Save(outputFilePath, ImageFormat.Png);

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

    static Bitmap ConvertToBitmap(byte[] raster, int width, int height, int bytesPerSample)
    {
        PixelFormat pixelFormat;
        if (bytesPerSample == 1)
        {
            pixelFormat = PixelFormat.Format8bppIndexed; // 8-bit grayscale
        }
        else if (bytesPerSample == 3)
        {
            pixelFormat = PixelFormat.Format24bppRgb; // 24-bit RGB
        }
        else if (bytesPerSample == 4)
        {
            pixelFormat = PixelFormat.Format32bppArgb; // 32-bit ARGB
        }
        else
        {
            throw new NotSupportedException("Unsupported bytes per sample: " + bytesPerSample);
        }

        Bitmap bitmap = new Bitmap(width, height, pixelFormat);

        if (pixelFormat == PixelFormat.Format8bppIndexed)
        {
            ColorPalette palette = bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            bitmap.Palette = palette;
        }

        BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        IntPtr ptr = bmpData.Scan0;

        int stride = bmpData.Stride;
        byte[] pixelData = new byte[stride * bitmap.Height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int rasterIndex = (y * width + x) * bytesPerSample;
                int bitmapIndex = (y * stride) + (x * bytesPerSample);

                if (bytesPerSample == 1) // Grayscale
                {
                    byte value = raster[rasterIndex];
                    pixelData[bitmapIndex] = value;
                }
                else if (bytesPerSample == 3) // RGB
                {
                    pixelData[bitmapIndex + 2] = raster[rasterIndex + 0]; // R
                    pixelData[bitmapIndex + 1] = raster[rasterIndex + 1]; // G
                    pixelData[bitmapIndex + 0] = raster[rasterIndex + 2]; // B
                }
                else if (bytesPerSample == 4) // ARGB
                {
                    pixelData[bitmapIndex + 3] = raster[rasterIndex + 3]; // A
                    pixelData[bitmapIndex + 2] = raster[rasterIndex + 2]; // R
                    pixelData[bitmapIndex + 1] = raster[rasterIndex + 1]; // G
                    pixelData[bitmapIndex + 0] = raster[rasterIndex + 0]; // B
                }
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, ptr, pixelData.Length);
        bitmap.UnlockBits(bmpData);

        return bitmap;
    }



    static Bitmap ResizeBitmap(Bitmap original, float scaleFactor)
    {
        int newWidth = (int)(original.Width * scaleFactor);
        int newHeight = (int)(original.Height * scaleFactor);

        Bitmap resized = new Bitmap(newWidth, newHeight);

        using (Graphics g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(original, 0, 0, newWidth, newHeight);
        }

        return resized;
    }
}

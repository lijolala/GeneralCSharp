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
            string outputFilePath = Path.Combine(outputFolder, $"tile_{tileX}_{tileY}.jpg");
            originalBitmap.Save(outputFilePath, ImageFormat.Jpeg);

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
        string base64String = Convert.ToBase64String(raster);
        Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
            bitmap.PixelFormat);

        // Copy the buffer data into the bitmap's pixel buffer
        System.Runtime.InteropServices.Marshal.Copy(raster, 0, bmpData.Scan0, raster.Length);

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

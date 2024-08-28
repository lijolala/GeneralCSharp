using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFolderPath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpExtent";
        int tileWidth = 256; // Width of each output tile
        int tileHeight = 256; // Height of each output tile

        // Example bounding box in geographic coordinates
        double minXGeo = 440720;
        double maxXGeo = 441720;
        double minYGeo = 3750320;
        double maxYGeo = 3751320;

        using (Tiff image = Tiff.Open(tiffFilePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            if (!image.IsTiled())
            {
                Console.WriteLine("The TIFF image is not tiled.");
                return;
            }

            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int tileSizeX = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileSizeY = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
            tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Ensure tile size is valid
            if (tileSizeX == 0 || tileSizeY == 0)
            {
                Console.WriteLine("Error: Tile size cannot be zero.");
                return;
            }

            //if (samplesPerPixel != 3 || bitsPerSample != 8)
            //{
            //    Console.WriteLine("This example assumes 3 samples per pixel (RGB) and 8 bits per sample.");
            //    return;
            //}

            // Convert the bounding box to pixel coordinates
            double[] geoTransform = GetGeoTransform(tiffFilePath);
            if (geoTransform == null || geoTransform.Length != 6 || geoTransform[1] == 0 || geoTransform[5] == 0)
            {
                Console.WriteLine("Error: Invalid GeoTransform or pixel size values are zero.");
                return;
            }

            int startX = ToPixelX(geoTransform, minXGeo);
            int endX = ToPixelX(geoTransform, maxXGeo);
            int startY = ToPixelY(geoTransform, maxYGeo); // Note: Y is inverted
            int endY = ToPixelY(geoTransform, minYGeo);   // Note: Y is inverted

            // Calculate the number of tiles horizontally and vertically
            int numTilesX = (int)Math.Ceiling((double)(endX - startX) / tileWidth);
            int numTilesY = (int)Math.Ceiling((double)(endY - startY) / tileHeight);

            for (int tileY = 0; tileY < numTilesY; tileY++)
            {
                for (int tileX = 0; tileX < numTilesX; tileX++)
                {
                    int currentTileStartX = startX + tileX * tileWidth;
                    int currentTileStartY = startY + tileY * tileHeight;
                    int currentTileWidth = Math.Min(tileWidth, endX - currentTileStartX);
                    int currentTileHeight = Math.Min(tileHeight, endY - currentTileStartY);

                    // Create a buffer to store the tile data
                    byte[] buffer = new byte[tileSizeX * tileSizeY * samplesPerPixel];

                    using (Bitmap tileImage = new Bitmap(currentTileWidth, currentTileHeight, PixelFormat.Format24bppRgb))
                    {
                        for (int y = 0; y < currentTileHeight; y += tileSizeY)
                        {
                            for (int x = 0; x < currentTileWidth; x += tileSizeX)
                            {
                                int tileOffsetX = (currentTileStartX + x) / tileSizeX;
                                int tileOffsetY = (currentTileStartY + y) / tileSizeY;

                                // Read the tile from the TIFF image
                                image.ReadTile(buffer, 0, tileOffsetX * tileSizeX, tileOffsetY * tileSizeY, 0, 0);

                                int bufferIndex = 0;
                                for (int tileYLocal = 0; tileYLocal < tileSizeY && (y + tileYLocal) < currentTileHeight; tileYLocal++)
                                {
                                    for (int tileXLocal = 0; tileXLocal < tileSizeX && (x + tileXLocal) < currentTileWidth; tileXLocal++)
                                    {
                                        try
                                        {
                                            int r = buffer[bufferIndex];
                                            int g = buffer[bufferIndex + 1];
                                            int b = buffer[bufferIndex + 2];

                                            tileImage.SetPixel(x + tileXLocal, y + tileYLocal, Color.FromArgb(r, g, b));
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error setting pixel at ({x + tileXLocal},{y + tileYLocal}): {ex.Message}");
                                        }

                                        bufferIndex += samplesPerPixel;
                                    }
                                }
                            }
                        }

                        // Save the tile image to a file
                        string tileFilePath = Path.Combine(outputFolderPath, $"tile_{tileX}_{tileY}.png");
                        tileImage.Save(tileFilePath, ImageFormat.Png);
                    }
                }
            }

            Console.WriteLine("Tiles created successfully.");
        }
    }

    static double[] GetGeoTransform(string tiffFilePath)
    {
        // This function should return the GeoTransform array. Implement as discussed earlier.
        return new double[] { 440720, 60, 0, 3751320, 0, -60 }; // Example GeoTransform
    }

    static int ToPixelX(double[] geoTransform, double geoX)
    {
        if (geoTransform[1] == 0) throw new DivideByZeroException("GeoTransform[1] cannot be zero.");
        return (int)((geoX - geoTransform[0]) / geoTransform[1]);
    }

    static int ToPixelY(double[] geoTransform, double geoY)
    {
        if (geoTransform[5] == 0) throw new DivideByZeroException("GeoTransform[5] cannot be zero.");
        return (int)((geoY - geoTransform[3]) / geoTransform[5]);
    }
}

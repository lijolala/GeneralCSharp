using System;

using BitMiracle.LibTiff.Classic;

class GeoTiffTileExtractor
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";

        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open the TIFF file.");
                return;
            }

            int zoomLevel = 1;

            // Loop through all directories (each directory corresponds to a zoom level)
            do
            {
                Console.WriteLine($"\nZoom Level {zoomLevel}:");

                // Get tile size
                FieldValue[] tileWidthTag = image.GetField(TiffTag.TILEWIDTH);
                FieldValue[] tileHeightTag = image.GetField(TiffTag.TILELENGTH);

                if (tileWidthTag != null && tileHeightTag != null)
                {
                    int tileWidth = tileWidthTag[0].ToInt();
                    int tileHeight = tileHeightTag[0].ToInt();
                    Console.WriteLine($"Tile Size: {tileWidth} x {tileHeight}");

                    // Calculate the number of tiles horizontally and vertically
                    int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    int imageLength = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                    int tileCountX = (imageWidth + tileWidth - 1) / tileWidth;
                    int tileCountY = (imageLength + tileHeight - 1) / tileHeight;
                    Console.WriteLine($"Tile Count: {tileCountX} x {tileCountY}");

                    // Iterate through tiles
                    for (int tileY = 0; tileY < tileCountY; tileY++)
                    {
                        for (int tileX = 0; tileX < tileCountX; tileX++)
                        {
                            // Calculate tile index
                            int tileIndex = image.ComputeTile(tileX * tileWidth, tileY * tileHeight, 0, 0);

                            // Buffer to store tile data
                            byte[] tileData = new byte[image.TileSize()];
                            image.ReadEncodedTile(tileIndex, tileData, 0, tileData.Length);

                            // Process the tile data as needed
                            Console.WriteLine($"Processing tile {tileX},{tileY} at Zoom Level {zoomLevel}...");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("This directory does not contain tiled data.");
                }

                zoomLevel++;
            } while (image.ReadDirectory());

            Console.WriteLine("\nFinished processing all zoom levels.");
        }

        Console.ReadLine();
    }
}

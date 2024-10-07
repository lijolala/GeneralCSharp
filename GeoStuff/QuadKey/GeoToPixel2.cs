using System;
using System.Drawing;

public class Program
{
    const int TileSize = 256; // Standard tile size for map tiles

    public static void Main()
    {
        // Input QuadKey (for example, a tile at zoom level 4)
        string quadKey = "1233";

        // Load the source map image (this may represent part of the world map at the given zoom level)
        Bitmap sourceImage = new Bitmap(@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\WarImage.png");

        // Get the size of the source image
        int sourceImageWidth = sourceImage.Width;
        int sourceImageHeight = sourceImage.Height;

        // Step 1: Convert QuadKey to tile coordinates (X, Y) and zoom level
        (int tileX, int tileY, int zoomLevel) = QuadKeyToTileXY(quadKey);

        Console.WriteLine($"TileX: {tileX}, TileY: {tileY}, Zoom Level: {zoomLevel}");

        // Step 2: Calculate the full world map size at the given zoom level
        double fullMapWidth = TileSize * Math.Pow(2, zoomLevel);
        double fullMapHeight = fullMapWidth; // The map is square

        // Step 3: Scale tile coordinates based on the size of the source image
        double scaleX = (double)sourceImageWidth / fullMapWidth;
        double scaleY = (double)sourceImageHeight / fullMapHeight;

        // Scaled pixel coordinates
        int tilePixelX = (int)(tileX * TileSize * scaleX);
        int tilePixelY = (int)(tileY * TileSize * scaleY);
        int scaledTileSizeX = (int)(TileSize * scaleX);
        int scaledTileSizeY = (int)(TileSize * scaleY);

        // Step 4: Ensure the tile is within the source image bounds
        if (tilePixelX + scaledTileSizeX > sourceImage.Width || tilePixelY + scaledTileSizeY > sourceImage.Height)
        {
            Console.WriteLine("The tile is outside the image bounds.");
            return;
        }

        // Step 5: Define the crop area for the tile
        Rectangle cropArea = new Rectangle(tilePixelX, tilePixelY, scaledTileSizeX, scaledTileSizeY);

        // Step 6: Crop the image
        Bitmap tileImage = new Bitmap(scaledTileSizeX, scaledTileSizeY);
        using (Graphics g = Graphics.FromImage(tileImage))
        {
            g.DrawImage(sourceImage, new Rectangle(0, 0, scaledTileSizeX, scaledTileSizeY), cropArea, GraphicsUnit.Pixel);
        }

        // Step 7: Resize the cropped tile to 256x256 pixels
        Bitmap resizedTileImage = new Bitmap(tileImage, new Size(TileSize, TileSize));
        resizedTileImage.Save($"tile_{quadKey}.png");

        Console.WriteLine("Tile saved successfully.");
    }

    // Converts a QuadKey to tile coordinates (X, Y) and zoom level
    public static (int tileX, int tileY, int zoomLevel) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0;
        int tileY = 0;
        int zoomLevel = quadKey.Length;

        for (int i = 0; i < quadKey.Length; i++)
        {
            int mask = 1 << (quadKey.Length - i - 1);
            switch (quadKey[i])
            {
                case '0':
                    // Top-left quadrant (do nothing, X and Y remain the same)
                    break;
                case '1':
                    // Top-right quadrant (increase X)
                    tileX |= mask;
                    break;
                case '2':
                    // Bottom-left quadrant (increase Y)
                    tileY |= mask;
                    break;
                case '3':
                    // Bottom-right quadrant (increase both X and Y)
                    tileX |= mask;
                    tileY |= mask;
                    break;
                default:
                    throw new ArgumentException($"Invalid QuadKey character: {quadKey[i]}");
            }
        }
        return (tileX, tileY, zoomLevel);
    }
}

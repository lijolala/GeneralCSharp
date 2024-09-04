using BitMiracle.LibTiff.Classic;


using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class GeoTiffTileExtractor
{
    public static void ExtractAndSaveTiles(string inputFilePath, string outputFilePath, double minX, double minY, double maxX, double maxY)
    {
        // Load the GeoTIFF file
        using (Tiff image = Tiff.Open(inputFilePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine("Could not open GeoTIFF file.");
                return;
            }

            // Read image dimensions
            int width = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            // Get the tile size
            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            // Check if GEOTIEPOINTS and GEOPIXELSCALE tags are available
            FieldValue[] tiePointField = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
            FieldValue[] pixelScaleField = image.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);

            // Get the georeferencing information (assumes North-Up image)
            double[] tiePoints = tiePointField[1].ToDoubleArray();
            double[] pixelScale = pixelScaleField[1].ToDoubleArray();

            // Calculate the pixel coordinates for the given map extent
            int minPixelX = (int)((minX - tiePoints[3]) / pixelScale[0]);
            int maxPixelX = (int)((maxX - tiePoints[3]) / pixelScale[0]);
            int minPixelY = (int)((tiePoints[4] - maxY) / pixelScale[1]);
            int maxPixelY = (int)((tiePoints[4] - minY) / pixelScale[1]);

            // Ensure the pixel coordinates are within image bounds
            minPixelX = Math.Max(0, minPixelX);
            maxPixelX = Math.Min(width - 1, maxPixelX);
            minPixelY = Math.Max(0, minPixelY);
            maxPixelY = Math.Min(height - 1, maxPixelY);

            // Create a Bitmap to store the extracted tiles
            int outputWidth = Math.Abs(maxPixelX - minPixelX + 1);
            int outputHeight = Math.Abs(maxPixelY - minPixelY + 1);
            using (Bitmap outputImage = new Bitmap(outputWidth, outputHeight , PixelFormat.Format24bppRgb))
            {
                for (int y = minPixelY; y <= maxPixelY; y += tileHeight)
                {
                    for (int x = minPixelX; x <= maxPixelX; x += tileWidth)
                    {
                        int tileX = x / tileWidth;
                        int tileY = y / tileHeight;

                        // Read the tile
                        byte[] tileData = new byte[tileWidth * tileHeight * 3]; // Assuming 24-bit RGB
                        image.ReadTile(tileData, 0, x, y, 0, 0);

                        // Copy tile data to the output image
                        for (int ty = 0; ty < tileHeight; ty++)
                        {
                            for (int tx = 0; tx < tileWidth; tx++)
                            {
                                int destX = x - minPixelX + tx;
                                int destY = y - minPixelY + ty;

                                if (destX < outputWidth && destY < outputHeight)
                                {
                                    int srcIndex = (ty * tileWidth + tx) * 3;
                                    Color color = Color.FromArgb(tileData[srcIndex], tileData[srcIndex + 1], tileData[srcIndex + 2]);
                                    outputImage.SetPixel(destX, destY, color);
                                }
                            }
                        }
                    }
                }
               
                // Save the output image as JPEG
                outputImage.Save(outputFilePath, ImageFormat.Jpeg);
            }
        }
    }

    static void Main(string[] args)
    {
        string inputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\output.jpg";
        double minX = 100.0; // example extent
        double minY = 100.0;
        double maxX = 200.0;
        double maxY = 200.0;

        ExtractAndSaveTiles(inputFilePath, outputFilePath, minX, minY, maxX, maxY);
    }
}

using Aspose.Imaging;
using Aspose.Imaging.ImageOptions;
using Aspose.Imaging.FileFormats.Tiff;
using Aspose.Imaging.FileFormats.Tiff.Enums;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Load the GeoTIFF image
        using (TiffImage tiffImage = (TiffImage)Image.Load("path_to_geotiff.tif"))
        {
            // Tile dimensions
            int tileWidth = 256;
            int tileHeight = 256;

            // Get image dimensions
            int rows = (int)Math.Ceiling((double)tiffImage.Height / tileHeight);
            int cols = (int)Math.Ceiling((double)tiffImage.Width / tileWidth);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = col * tileWidth;
                    int y = row * tileHeight;
                    int width = Math.Min(tileWidth, tiffImage.Width - x);
                    int height = Math.Min(tileHeight, tiffImage.Height - y);

                    // Define the rectangle for the tile
                    Rectangle tileRect = new Rectangle(x, y, width, height);

                    // Create a new blank image for the tile
                    using (RasterImage tileImage = new RasterImage(width, height))
                    {
                        tileImage.SaveOptions = new TiffOptions(TiffExpectedFormat.Default);

                        // Use graphics to draw the tile region from the original image
                        using (Graphics g = new Graphics(tileImage))
                        {
                            g.DrawImage(tiffImage, 0, 0, tileRect);
                        }

                        // Save the tile
                        string tileFileName = $"tile_{row}_{col}.tif";
                        tileImage.Save(tileFileName, new TiffOptions(TiffExpectedFormat.TiffLzwRgb));
                    }
                }
            }
        }
    }
}

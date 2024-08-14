using System;
using System.Drawing;
using System.IO;

class TiffImageReconstructor
{
    static void Main(string[] args)
    {
        string inputFolder = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump"; // Folder containing the tile JPEGs
        string outputImagePath = "reconstructed_image.jpeg"; // Path to save the reconstructed image

        // Set these values according to the tile size and the number of tiles
        int tileWidth = 512;  // Example tile width
        int tileHeight = 512; // Example tile height
        int tilesAcross = 141; // Number of tiles across (columns)
        int tilesDown = 71;   // Number of tiles down (rows)

        // Calculate the size of the final image
        int finalWidth = tileWidth * tilesAcross;
        int finalHeight = tileHeight * tilesDown;

        // Create a new bitmap to hold the reconstructed image
        using (Bitmap finalImage = new Bitmap(finalWidth, finalHeight))
        {
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                // Clear the canvas
                g.Clear(Color.White);

                // Iterate over the tiles
                for (int row = 0; row < tilesDown; row++)
                {
                    for (int col = 0; col < tilesAcross; col++)
                    {
                        string tilePath = Path.Combine(inputFolder, $"tile_{row}_{col}.jpeg");

                        if (File.Exists(tilePath))
                        {
                            using (Bitmap tile = new Bitmap(tilePath))
                            {
                                // Calculate the position of the tile in the final image
                                int xPos = col * tileWidth;
                                int yPos = row * tileHeight;

                                // Draw the tile on the final image
                                g.DrawImage(tile, new Rectangle(xPos, yPos, tileWidth, tileHeight));
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Tile {row}_{col} not found.");
                        }
                    }
                }
            }

            // Save the final reconstructed image
            finalImage.Save(outputImagePath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        Console.WriteLine("Reconstructed image saved at " + outputImagePath);
    }
}


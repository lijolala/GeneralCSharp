using System;
using System.Drawing;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

public class GeoTiffViewer : Form
{
    private PictureBox pictureBox;

    public GeoTiffViewer()
    {
        // Initialize UI components
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        this.Controls.Add(pictureBox);

        // Load and display the GeoTIFF with scaling
        LoadGeoTiff(@"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif", 0.1); // Scale to 10% of original size
    }

    private void LoadGeoTiff(string filePath, double scale)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            int originalWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int originalHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int scaledWidth = (int)(originalWidth * scale);
            int scaledHeight = (int)(originalHeight * scale);
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int bytesPerSample = bitsPerSample / 8;
            int stride = originalWidth * samplesPerPixel * bytesPerSample;

            // Allocate byte array for the full-size raster data
            byte[] raster = new byte[originalHeight * stride];

            // Read the raster data into the byte array
            for (int row = 0; row < originalHeight; row++)
            {
                image.ReadScanline(raster, row * stride, row, 0);
            }

            // Create a Bitmap for the scaled image
            Bitmap scaledBitmap = new Bitmap(scaledWidth, scaledHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Rescale and copy the raster data to the scaled Bitmap
            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    int origX = Math.Min((int)(x / scale), originalWidth - 1);
                    int origY = Math.Min((int)(y / scale), originalHeight - 1);
                    int offset = origY * stride + origX * samplesPerPixel * bytesPerSample;

                    Color color = Color.FromArgb(
                        samplesPerPixel > 2 ? raster[offset + 2] : raster[offset], // Blue or Red
                        samplesPerPixel > 1 ? raster[offset + 1] : raster[offset], // Green or Gray
                        raster[offset]); // Red or Gray

                    scaledBitmap.SetPixel(x, y, color);
                }
            }

            // Set the scaled bitmap to the PictureBox
            pictureBox.Image = scaledBitmap;
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new GeoTiffViewer());
    }
}

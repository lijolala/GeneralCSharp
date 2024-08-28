using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BitMiracle.LibTiff.Classic;

public class GeoTiffViewer : Form
{
    private PictureBox pictureBox;

    public GeoTiffViewer()
    {
        pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        this.Controls.Add(pictureBox);

        LoadGeoTiff(@"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\la.tif");
    }

    private void LoadGeoTiff(string filePath)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {
            if (image == null)
            {
                Console.WriteLine();
                return;
            }

            int originalWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int originalHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
            int samplesPerPixel = image.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
            int bitsPerSample = image.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
            int bytesPerSample = bitsPerSample / 8;
            int stride = originalWidth * samplesPerPixel * bytesPerSample;
            int photometric = image.GetField(TiffTag.PHOTOMETRIC)[0].ToInt();

            Console.WriteLine($"Image Dimensions: {originalWidth}x{originalHeight}");
            Console.WriteLine($"Samples Per Pixel: {samplesPerPixel}, Bits Per Sample: {bitsPerSample}");
            Console.WriteLine($"Photometric Interpretation: {photometric}");

            // Ensure valid photometric interpretation
            if (photometric != (int)Photometric.RGB)
            {
                Console.WriteLine("Unsupported Photometric Interpretation.");
                return;
            }

            // Allocate byte array for the raster data
            byte[] raster = new byte[originalHeight * stride];
            for (int row = 0; row < originalHeight; row++)
            {
                bool success = image.ReadScanline(raster, row * stride, row, 0);
                if (!success)
                {
                    Console.WriteLine($"Failed to read scanline at row {row}");
                }
            }

            // Create a Bitmap for a small section of the image
            int testWidth = Math.Min(originalWidth, 100); // Limit to a small section
            int testHeight = Math.Min(originalHeight, 100); // Limit to a small section
            Bitmap testBitmap = new Bitmap(testWidth, testHeight, PixelFormat.Format24bppRgb);

            BitmapData bmpData = testBitmap.LockBits(new Rectangle(0, 0, testBitmap.Width, testBitmap.Height),
                                                     ImageLockMode.WriteOnly, testBitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(testBitmap.PixelFormat) / 8;
            int strideBitmap = bmpData.Stride;
            byte[] testPixels = new byte[strideBitmap * testHeight];

            for (int y = 0; y < testHeight; y++)
            {
                for (int x = 0; x < testWidth; x++)
                {
                    int offset = (y * stride) + (x * samplesPerPixel * bytesPerSample);

                    if (offset + samplesPerPixel * bytesPerSample <= raster.Length)
                    {
                        int pixelIndex = (y * strideBitmap) + (x * bytesPerPixel);

                        // Check if the values are within bounds
                        if (offset + 2 < raster.Length)
                        {
                            testPixels[pixelIndex] = raster[offset + 2];    // Blue
                            testPixels[pixelIndex + 1] = raster[offset + 1]; // Green
                            testPixels[pixelIndex + 2] = raster[offset];     // Red
                        }
                    }
                }
            }

            // Copy the modified pixel data back to the Bitmap
            Marshal.Copy(testPixels, 0, bmpData.Scan0, testPixels.Length);
            testBitmap.UnlockBits(bmpData);

            // Set the image to the PictureBox
            pictureBox.Image = testBitmap;
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new GeoTiffViewer());
    }
}

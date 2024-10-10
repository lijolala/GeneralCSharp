using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public class Program
{
    public static void Main()
    {
        // Example byte array representing a PNG image
        byte[] imageBytes = File.ReadAllBytes(@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\WarImage.png");

        // Convert byte array to image and save it
        Image image = ByteArrayToImage(imageBytes);

        // Save the image as a PNG file
        image.Save("output_image.png", ImageFormat.Png);

        Console.WriteLine("Image saved successfully.");
    }

    // Method to convert byte array to Image
    public static Image ByteArrayToImage(byte[] byteArray)
    {
        using (MemoryStream ms = new MemoryStream(byteArray))
        {
            // Create Image from the MemoryStream
            return Image.FromStream(ms);
        }
    }
}
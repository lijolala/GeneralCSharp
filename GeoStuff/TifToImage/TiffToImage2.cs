
using System;
using System.Drawing.Imaging;
using System.Linq;
using NetVips;

class Program
{
    static void Main(string[] args)
    {
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";   // Path to your GeoTIFF file
        string pngFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\WarImage.png";   // Path for the output PNG
        if (!System.IO.File.Exists(tiffFilePath))
        {
            Console.WriteLine("TIFF file does not exist.");
            return;
        }
        var image = Image.NewFromFile(tiffFilePath , access: Enums.Access.Sequential);

        var encoders = ImageCodecInfo.GetImageEncoders();
        var imageCodecInfo = encoders.FirstOrDefault(encoder => encoder.MimeType == "image/tiff");

        if (imageCodecInfo == null)
        {
            return;
        }

        var imageEncoderParams = new EncoderParameters(1);
        imageEncoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
        image.WriteToFile(pngFilePath, new VOption
        {
            {"strip", true},
            {"compression",  0}
        });


        Console.WriteLine($"Conversion successful! Image saved at {pngFilePath}");
    }
}
using OSGeo.GDAL;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class GeoTiffRangeToImageConverter
{
    private static readonly HttpClient _httpClient = new HttpClient();

    static GeoTiffRangeToImageConverter()
    {
        Gdal.AllRegister();  // Register all GDAL drivers
    }

    public static async Task<byte[]> FetchGeoTiffRangeAsync(string url, long startByte, long endByte)
    {
        _httpClient.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);
        HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            throw new HttpRequestException($"Failed to fetch the range. Status code: {response.StatusCode}");
        }
    }

    public static Bitmap ConvertGeoTiffBytesToImage(byte[] geoTiffData)
    {
        if (geoTiffData == null || geoTiffData.Length == 0)
        {
            throw new ArgumentException("GeoTIFF data is null or empty.");
        }

        // Create a temporary file to store the GeoTIFF data
        string tempFilePath = Path.GetTempFileName();
        File.WriteAllBytes(tempFilePath, geoTiffData);

        // Open the file with GDAL
        var dataset = Gdal.Open(tempFilePath, Access.GA_ReadOnly);
        if (dataset == null)
        {
            throw new InvalidOperationException("Failed to open the GeoTIFF data.");
        }

        // Read the raster bands and convert them to a bitmap
        var rasterBand = dataset.GetRasterBand(1); // Assumes single-band for simplicity
        int width = rasterBand.XSize;
        int height = rasterBand.YSize;

        var buffer = new byte[width * height];
        rasterBand.ReadRaster(0, 0, width, height, buffer, width, height, 0, 0);

        var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        bitmap.UnlockBits(data);

        // Cleanup: Delete the temporary file
        File.Delete(tempFilePath);

        return bitmap;
    }

    public static void SaveImage(Bitmap image, string filePath)
    {
        image.Save(filePath, ImageFormat.Jpeg);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        string url = "http://oin-hotosm.s3.amazonaws.com/59c66c5223c8440011d7b1e4/0/7ad397c0-bba2-4f98-a08a-931ec3a6e943.tif"; // Replace with your GeoTIFF/COG URL
        long startByte = 0; // Adjust based on the range you want to fetch
        long endByte = 1023; // Adjust based on the range you want to fetch

        try
        {
            // Fetch the byte range from the GeoTIFF
            byte[] geoTiffData = await GeoTiffRangeToImageConverter.FetchGeoTiffRangeAsync(url, startByte, endByte);

            // Convert the GeoTIFF byte array to a Bitmap
            Bitmap image = GeoTiffRangeToImageConverter.ConvertGeoTiffBytesToImage(geoTiffData);

            // Save the Image to a file
            string outputPath = "output.jpg";
            GeoTiffRangeToImageConverter.SaveImage(image, outputPath);

            Console.WriteLine($"Image successfully saved to {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}

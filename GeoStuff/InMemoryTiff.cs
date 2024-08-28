using System;
using System.IO;
using System.Net.Http;

using OSGeo.GDAL;

class Program
{
    static async System.Threading.Tasks.Task Main(string[] args)
    {
        string url = "YOUR_GEO_TIFF_URL_HERE";

        // Initialize GDAL
        Gdal.AllRegister();

        // Temporary file path
        string tempFilePath = Path.Combine(Path.GetTempPath(), "temp_geotiff.tif");

        try
        {
            // Download the GeoTIFF file into a memory stream
            using (HttpClient client = new HttpClient())
            {
                using (Stream stream = await client.GetStreamAsync(url))
                {
                    using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }

            // Open the dataset from the temporary file
            using (Dataset dataset = Gdal.Open(tempFilePath, Access.GA_ReadOnly))
            {
                if (dataset == null)
                {
                    Console.WriteLine("Failed to open dataset.");
                    return;
                }

                // Get metadata
                string[] metadata = dataset.GetMetadata("");

                Console.WriteLine("Metadata:");
                foreach (var item in metadata)
                {
                    Console.WriteLine(item);
                }

                // Get some basic information about the GeoTIFF
                Console.WriteLine("Driver: " + dataset.GetDriver().LongName);
                Console.WriteLine("Size: " + dataset.RasterXSize + " x " + dataset.RasterYSize);
            }
        }
    }
}
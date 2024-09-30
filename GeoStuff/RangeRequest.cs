using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;

class ProgramRangeRequest
{
    static async Task Main(string[] args)
    {
        string url = "http://127.0.0.1:5500/cogs/la.tif"; // Replace with your COG URL
        long startByte = 0;
        long endByte = 1023; // Example: first 1024 bytes
        long fileSize = await GetFileSizeAsync(url);
        Console.WriteLine($"Received {fileSize} bytes from the COG.");
        // Fetch the byte data
        byte[] imageData = await FetchCOGRangeAsync(url, startByte, endByte);
        Console.WriteLine($"Received {imageData.Length} bytes from the COG.");
        
        // Convert the byte data to an image
        using (MemoryStream ms = new MemoryStream(imageData))
        {
            System.Drawing.i
             MediaTypeNames.Image image = MediaTypeNames.Image.FromStream(ms); // Use System.Drawing.Image here

            // Display the image (e.g., save it to disk or use in another context)
            image.Save("output.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            Console.WriteLine("Image saved as output.jpg");
        }
    }

    static async Task<byte[]> FetchCOGRangeAsync(string url, long startByte, long endByte)
    {
        using (HttpClient client = new HttpClient())
        {
            // Set up the range header
            client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);

            // Send the request
            HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                // Read the content as a byte array
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                throw new HttpRequestException($"Failed to fetch the range. Status code: {response.StatusCode}");
            }
        }
    }

    static async Task<long> GetFileSizeAsync(string url)
    {
        using (HttpClient client = new HttpClient())
        {
            // Send a HEAD request to get headers only
            HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

            if (response.IsSuccessStatusCode)
            {
                // Get the Content-Length header, which contains the file size
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    return response.Content.Headers.ContentLength.Value;
                }
            }

            // Return -1 if unable to retrieve the size
            return -1;
        }
    }
}
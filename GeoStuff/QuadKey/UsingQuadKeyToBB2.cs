using System;
using System.Drawing;
using System.Drawing.Imaging;
using BitMiracle.LibTiff.Classic;

class QuadKeyToGeoTIFF
{
    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, int level) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int level = quadKey.Length;

        for (int i = level; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[level - i])
            {
                case '0':
                    break;
                case '1':
                    tileX |= mask;
                    break;
                case '2':
                    tileY |= mask;
                    break;
                case '3':
                    tileX |= mask;
                    tileY |= mask;
                    break;
                default:
                    throw new ArgumentException("Invalid QuadKey digit.");
            }
        }
        return (tileX, tileY, level);
    }

    // Convert Pixel Coordinates to Latitude and Longitude
    public static (double lat, double lon) PixelXYToLatLong(int pixelX, int pixelY, int level)
    {
        double mapSize = 256 << level;
        double x = (pixelX / mapSize) - 0.5;
        double y = 0.5 - (pixelY / mapSize);

        double lat = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
        double lon = 360 * x;

        return (lat, lon);
    }

    // Convert Tile Coordinates to Bounding Box (Lat/Lon)
    public static (double north, double south, double east, double west) TileXYToBoundingBox(int tileX, int tileY, int level)
    {
        double n = Math.Pow(2.0, level);

        double lonMin = tileX / n * 360.0 - 180.0;
        double lonMax = (tileX + 1) / n * 360.0 - 180.0;

        double latMinRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
        double latMaxRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n)));

        double latMin = latMinRad * (180.0 / Math.PI);
        double latMax = latMaxRad * (180.0 / Math.PI);

        return (lonMin, latMin, lonMax, latMax);
    }

    // Read GeoTIFF Metadata using libtiff
    public static byte[] ReadGeoTiff(string filePath, int zoomLevel, double minLat, double minLon, double maxLat,
        double maxLon, out int boxWidth, out int boxHeight)
    {
        using (Tiff image = Tiff.Open(filePath, "r"))
        {

            if (image.NumberOfDirectories() > zoomLevel)
            {
                image.SetDirectory((short)zoomLevel);
            }
            else
            {
                image.SetDirectory((short)image.NumberOfDirectories());
            }

            const int ModelTiepointTag = 33922;
            const int ModelPixelScaleTag = 33550;
            // Fetch the model pixel scale tag (scaling)
            //FieldValue[] pixelScaleValues = image.GetField((TiffTag)ModelPixelScaleTag);
            //double[] modelPixelScaleTag = { 1.0, 1.0 }; // Default pixel scales
            //byte[] byteArray = pixelScaleValues[1].GetBytes();
            //modelPixelScaleTag = ByteArrayToDoubleArray(byteArray);
            //if (modelPixelScaleTag != null)
            //{
            //    double scaleX = modelPixelScaleTag[0];
            //    double scaleY = modelPixelScaleTag[1];
            //    Console.WriteLine($"ScaleX: {scaleX}, ScaleY: {scaleY}");
            //}

            //// Fetch the model tiepoint tag (mapping between image space and geographical space)
            //FieldValue[] modelTiepointTag = image.GetField((TiffTag)ModelTiepointTag);
            //if (modelTiepointTag != null)
            //{
            //    byte[] tiePointsArray = modelTiepointTag[1].GetBytes();
            //    double[] tiePoints = ByteArrayToDoubleArray(tiePointsArray);
            //    double imageX = tiePoints[0];
            //    double imageY = tiePoints[1];
            //    double imageZ = tiePoints[2];
            //    double geoX = tiePoints[3];
            //    double geoY = tiePoints[4];
            //    double geoZ = tiePoints[5];

            //    Console.WriteLine($"Image (X, Y, Z): ({imageX}, {imageY}, {imageZ})");
            //    Console.WriteLine($"Geo (X, Y, Z): ({geoX}, {geoY}, {geoZ})");
            //}

            int imageWidth = image.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int imageHeight = image.GetField(TiffTag.IMAGELENGTH)[0].ToInt();



            //// Get the tile size
            //int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            //int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

            double[] pixelScale = { 0.005000000000002559689, -0.004999999999999006108 };
            double[] tiePoint = { -180.09, 90.048 };

            // Convert bounding box coordinates to pixel coordinates
            var (pixelXMin, pixelYMin) = GeoToPixel(minLat, minLon, tiePoint, pixelScale);
            var (pixelXMax, pixelYMax) = GeoToPixel(maxLat, maxLon, tiePoint, pixelScale);
            boxWidth = Math.Abs(pixelXMax - pixelXMin);
            boxHeight = Math.Abs(pixelYMax - pixelYMin);
            pixelXMax = Math.Min(imageWidth, pixelXMax);
            pixelYMax = Math.Min(imageHeight, pixelYMax);
            // Calculate the size of the region to extract
            int width = pixelXMax - pixelXMin;
            int height = pixelYMax - pixelYMin;
            byte[] buffer = new byte[boxWidth * boxHeight * 4];
            // For RGBA
            //tiff.ReadRGBAImage(width, height, raster);
            // Read tiles that overlap with the bounding box
            //for (int y = 0; y < imageHeight; y += tileHeight)
            //{
            //    for (int x = 0; x < imageWidth; x += tileWidth)
            //    {
            //        // Get the tile data
            //        byte[] tileBuffer = new byte[tileWidth * tileHeight * 4]; // Assuming 4 samples (RGBA)
            //        tiff.ReadTile(tileBuffer, 0, x, y, 0, 0);

            //        // Determine if this tile intersects with the region of interest
            //        // Copy relevant pixels to the output buffer
            //        for (int ty = 0; ty < tileHeight; ty++)
            //        {
            //            int destY = y + ty;
            //            if (destY >= pixelYMin && destY < pixelYMax)
            //            {
            //                for (int tx = 0; tx < tileWidth; tx++)
            //                {
            //                    int destX = x + tx;
            //                    if (destX >= pixelXMin && destX < pixelXMax)
            //                    {
            //                        int destIndex = ((destY - pixelYMin) * boxWidth + (destX - pixelXMin)) * 4;
            //                        int srcIndex = (ty * tileWidth + tx) * 4;

            //                        buffer[destIndex] = tileBuffer[srcIndex];
            //                        buffer[destIndex + 1] = tileBuffer[srcIndex + 1];
            //                        buffer[destIndex + 2] = tileBuffer[srcIndex + 2];
            //                        buffer[destIndex + 3] = tileBuffer[srcIndex + 3];
            //                    }
            //                }
            //            }
            //        }
            //    }


            //}
            if (image.IsTiled())
            {
                // Read tiles that overlap with the bounding box
                int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();

                for (int y = 0; y < imageHeight; y += tileHeight)
                {
                    for (int x = 0; x < imageWidth; x += tileWidth)
                    {
                        // Allocate buffer for a single tile
                        byte[] tileBuffer = new byte[tileWidth * tileHeight * 4];  // Assuming 4 bands (RGBA)
                        image.ReadTile(tileBuffer, 0, x, y, 0, 0);

                        // Copy data from the tileBuffer into the output buffer if it falls within the bounding box
                        CopyTileDataToBuffer(tileBuffer, x, y, tileWidth, tileHeight, buffer, pixelXMin, pixelYMin, width, height);
                    }
                }
            }

            return buffer;
        }
        
    }
    public static void CopyTileDataToBuffer(byte[] tileBuffer, int tileX, int tileY, int tileWidth, int tileHeight, byte[] outputBuffer, int pixelXMin, int pixelYMin, int outputWidth, int outputHeight)
    {
        for (int ty = 0; ty < tileHeight; ty++)
        {
            int destY = tileY + ty;
            if (destY >= pixelYMin && destY < pixelYMin + outputHeight)
            {
                for (int tx = 0; tx < tileWidth; tx++)
                {
                    int destX = tileX + tx;
                    if (destX >= pixelXMin && destX < pixelXMin + outputWidth)
                    {
                        int outputIndex = ((destY - pixelYMin) * outputWidth + (destX - pixelXMin)) * 4;
                        int tileIndex = (ty * tileWidth + tx) * 4;

                        outputBuffer[outputIndex] = tileBuffer[tileIndex];
                        outputBuffer[outputIndex + 1] = tileBuffer[tileIndex + 1];
                        outputBuffer[outputIndex + 2] = tileBuffer[tileIndex + 2];
                        outputBuffer[outputIndex + 3] = tileBuffer[tileIndex + 3];
                    }
                }
            }
        }
    }
    public static void SaveBufferAsPng(byte[] buffer, int width, int height, string outputFilePath)
    {
        using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            // Copy buffer data into the bitmap
            int bufferIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Extract RGBA values from the buffer
                    byte r = buffer[bufferIndex];
                    byte g = buffer[bufferIndex + 1];
                    byte b = buffer[bufferIndex + 2];
                    byte a = buffer[bufferIndex + 3];

                    // Set pixel in the bitmap
                    Color pixelColor = Color.FromArgb(a, r, g, b);
                    bmp.SetPixel(x, y, pixelColor);

                    bufferIndex += 4; // Move to the next pixel (RGBA)
                }
            }

            // Save the bitmap as a PNG file
            bmp.Save(outputFilePath, ImageFormat.Png);
        }
    }

    public static (int pixelX, int pixelY) GeoToPixel(double lat, double lon, double[] tiePoint, double[] pixelScale)
    {
        double pixelX = (lon - tiePoint[0]) / pixelScale[0];
        double pixelY = (lat - tiePoint[1]) / -pixelScale[1]; // Note: y is flipped in many coordinate systems

        return ((int)Math.Round(pixelX), (int)Math.Round(pixelY));
    }

    static double[] ByteArrayToDoubleArray(byte[] byteArray)
    {
        int doubleSize = sizeof(double);
        int count = byteArray.Length / doubleSize;
        double[] result = new double[count];

        for (int i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToDouble(byteArray, i * doubleSize);
        }

        return result;
    }

    public static void Main(string[] args)
    {
        // Example quadkey
        string quadKey = "1202102332";
        string tiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; ;
        var (tileX, tileY, level) = QuadKeyToTileXY(quadKey);
        var (left, bottom, right, top) = TileXYToBoundingBox(tileX, tileY, level);

        Console.WriteLine($"QuadKey: {quadKey}");
        Console.WriteLine($"TileX: {tileX}, TileY: {tileY}, Level: {level}");
        //Console.WriteLine($"Bounding Box: North={north}, South={bottom}, East={east}, West={west}");

        //ReadGeoTiff(tiffFilePath, minLat, minLon, maxLat, maxLon);
        byte[] buffer =  ReadGeoTiff(tiffFilePath,level, left, bottom, right, top, out int boxWidth, out int boxHeight);
        // Read GeoTIFF file
        SaveBufferAsPng(buffer, boxWidth, boxHeight, outputFilePath);

    }
   
}

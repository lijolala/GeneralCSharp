using BitMiracle.LibTiff.Classic;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        string filePath =
            @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; // Replace with your GeoTIFF file path
        string quadKey = "2101"; // Replace with your QuadKey
        string outputTilePath =
            @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDump1\tile_output.png"; // Replace with your output tile path

        ExtractTileFromGeoTiff(filePath, quadKey, outputTilePath);
    }

    public static void ExtractTileFromGeoTiff(string geoTiffPath, string quadKey, string outputTilePath)
    {
        var (zoomLevel, tileX, tileY) = QuadKeyToTile(quadKey);
        var (minLon, minLat, maxLon, maxLat) = TileToBoundingBox(tileX, tileY, zoomLevel);

        // Convert the bounding box from geographic to Web Mercator coordinates
        (double minX, double minY) = GeoToWebMercator(minLon, minLat);
        (double maxX, double maxY) = GeoToWebMercator(maxLon, maxLat);

        using (Tiff image = Tiff.Open(geoTiffPath, "r"))
        {
            double[] geoTransform = GetGeoTransform(image);
            int minXPixel, minYPixel, maxXPixel, maxYPixel;

            // Convert the Web Mercator coordinates to pixel coordinates
            WebMercatorToPixel(minX, minY, geoTransform, out minXPixel, out minYPixel);
            WebMercatorToPixel(maxX, maxY, geoTransform, out maxXPixel, out maxYPixel);

            int tileWidth = image.GetField(TiffTag.TILEWIDTH)[0].ToInt();
            int tileHeight = image.GetField(TiffTag.TILELENGTH)[0].ToInt();
            byte[] tileBuffer = new byte[image.TileSize()];

            image.ReadTile(tileBuffer, 0, minXPixel, minYPixel, 0, 0);

            SaveTileAsPng(tileBuffer, tileWidth, tileHeight, outputTilePath);
        }
    }

    public static double[] GetGeoTransform(Tiff image)
    {
       // const int ModelPixelScaleTag = 33550;
        FieldValue[] pixelScaleValues = image.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
        double[] pixelScales = null;
        if (pixelScaleValues != null && pixelScaleValues.Length > 0)
        {
            byte[] byteArray = pixelScaleValues[1].GetBytes();
            pixelScales = ByteArrayToDoubleArray(byteArray);

            if (pixelScales.Length >= 2)
            {
                double scaleX = pixelScales[0];
                double scaleY = pixelScales[1];
                Console.WriteLine($"Pixel Scale - X: {scaleX}, Y: {scaleY}");
            }
            else
            {
                Console.WriteLine("Unexpected pixel scale array length.");
            }
        }

        const int ModelTiepointTag = 33922;
        double[] tiePoints = null;
        FieldValue[] tiePointsValues = image.GetField((TiffTag)ModelTiepointTag);
        if (tiePointsValues != null && tiePointsValues.Length > 0)
        {
            byte[] byteArray = tiePointsValues[1].GetBytes();
            tiePoints = ByteArrayToDoubleArray(byteArray);

            for (int i = 0; i < tiePoints.Length; i += 6)
            {
                double pixelX = tiePoints[i];
                double pixelY = tiePoints[i + 1];
                double geoX = tiePoints[i + 3];
                double geoY = tiePoints[i + 4];

                Console.WriteLine($"Tie Point - Pixel (X, Y): ({pixelX}, {pixelY}), Geo (X, Y): ({geoX}, {geoY})");
            }
        }

        if (tiePoints == null || tiePoints.Length < 6)
            throw new Exception("ModelTiePointTag is missing or incomplete.");

        double[] geoTransform = new double[6];
        geoTransform[0] = tiePoints[3];
        if (pixelScales != null)
        {
            geoTransform[1] = pixelScales[0];
            geoTransform[2] = 0;
            geoTransform[3] = tiePoints[4];
            geoTransform[4] = 0;
            geoTransform[5] = -pixelScales[1];
        }

        return geoTransform;
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

    /// <summary>
    /// Converts geographic coordinates to Web Mercator coordinates.
    /// </summary>
    public static (double x, double y) GeoToWebMercator(double lon, double lat)
    {
        double earthRadius = 6378137;
        double x = lon * (earthRadius * Math.PI / 180);
        double y = Math.Log(Math.Tan(Math.PI / 4 + lat * Math.PI / 360)) * earthRadius;
        return (x, y);
    }

    /// <summary>
    /// Converts Web Mercator coordinates to pixel coordinates using the GeoTransform.
    /// </summary>
    public static void WebMercatorToPixel(double mercX, double mercY, double[] geoTransform, out int pixelX,
        out int pixelY)
    {
        pixelX = (int)((mercX - geoTransform[0]) / geoTransform[1]);
        pixelY = (int)((mercY - geoTransform[3]) / geoTransform[5]);
    }

    public static void SaveTileAsPng(byte[] buffer, int tileWidth, int tileHeight, string outputPath)
    {
        using (Bitmap bitmap = new Bitmap(tileWidth, tileHeight, PixelFormat.Format32bppArgb))
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, tileWidth, tileHeight),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bitmap.UnlockBits(bmpData);

            SetBitmapTransparency(bitmap);
            bitmap.Save(outputPath, ImageFormat.Png);
        }
    }

    public static void SetBitmapTransparency(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                if (pixelColor.R == 255 && pixelColor.G == 255 && pixelColor.B == 255)
                {
                    Color transparentColor = Color.FromArgb(0, pixelColor.R, pixelColor.G, pixelColor.B);
                    bitmap.SetPixel(x, y, transparentColor);
                }
            }
        }
    }

    public static (int zoomLevel, int tileX, int tileY) QuadKeyToTile(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int zoomLevel = quadKey.Length;

        for (int i = zoomLevel; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[zoomLevel - i])
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

        return (zoomLevel, tileX, tileY);
    }

    public static (double minLon, double minLat, double maxLon, double maxLat) TileToBoundingBox(int tileX, int tileY,
        int zoomLevel)
    {
        double n = Math.Pow(2.0, zoomLevel);

        double lon1 = tileX / n * 360.0 - 180.0;
        double lon2 = (tileX + 1) / n * 360.0 - 180.0;
        double lat1 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n))) * (180.0 / Math.PI);
        double lat2 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n))) * (180.0 / Math.PI);

        return (lon1, lat2, lon2, lat1);
    }
}
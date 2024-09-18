using System;

using BitMiracle.LibTiff.Classic;


class GeoTiffZoomDirectory
{
    // Convert QuadKey to Tile Coordinates
    public static (int tileX, int tileY, int zoomLevel) QuadKeyToTileXY(string quadKey)
    {
        int tileX = 0, tileY = 0;
        int zoomLevel = quadKey.Length;

        for (int i = zoomLevel; i > 0; i--)
        {
            int mask = 1 << (i - 1);
            switch (quadKey[zoomLevel - i])
            {
                case '0': break;
                case '1': tileX |= mask; break;
                case '2': tileY |= mask; break;
                case '3': tileX |= mask; tileY |= mask; break;
                default: throw new ArgumentException("Invalid QuadKey digit.");
            }
        }
        return (tileX, tileY, zoomLevel);
    }

    // Get the correct directory (zoom level) from the GeoTIFF file
    public static void GetCorrectZoomDirectory(string geoTiffFilePath, int desiredZoomLevel)
    {
        using (Tiff tiff = Tiff.Open(geoTiffFilePath, "r"))
        {
            if (tiff == null)
            {
                throw new Exception("Could not open GeoTIFF file.");
            }

           
            // Loop through all directories in the GeoTIFF
            int directoryCount = tiff.NumberOfDirectories();
            for (int i = 0; i < directoryCount; i++)
            {
                tiff.SetDirectory((short)i);

                // Retrieve image width and height for each directory
                int imageWidth = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int imageLength = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                // Calculate the corresponding zoom level based on the image size
                int zoomLevel = (int)Math.Log(Math.Max(imageWidth, imageLength) / 256);

                if (zoomLevel == desiredZoomLevel)
                {
                    Console.WriteLine($"Found directory for zoom level {desiredZoomLevel}: Directory {i}");
                    // Now you can work with this directory, e.g., extract data or map coordinates
                    return;
                }
            }
            const int GeoKeyDirectoryTag = 34735;
            FieldValue[] geoCitation = tiff.GetField((TiffTag)GeoKeyDirectoryTag);

            Console.WriteLine($"Zoom level {desiredZoomLevel} not found in the GeoTIFF.");
        }
    }

    public static void Main(string[] args)
    {
        // Example QuadKey
        string quadKey = "120210233222";
        var rect = WebMercator.QuadTreeToLatLon(quadKey);
        var (tileX, tileY, zoomLevel) = QuadKeyToTileXY(quadKey);

        Console.WriteLine($"QuadKey: {quadKey}");
        Console.WriteLine($"TileX: {tileX}, TileY: {tileY}, ZoomLevel: {zoomLevel}");

        // Load GeoTIFF file and find the correct zoom directory
        string geoTiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";
        GetCorrectZoomDirectory(geoTiffFilePath, zoomLevel);
    }
}

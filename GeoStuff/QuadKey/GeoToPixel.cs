﻿using System;
using System.Drawing;

using TiffLibrary;

class Program
{
    static void Main()
    {
       

        string quadKey = "2103033";
        var (tileX, tileY, level) = RasterHelper.QuadKeyToTileXY(quadKey);
       // var (minLon, minLat, maxLon, maxLat) = RasterHelper.TileXYToBoundingBox(tileX, tileY, level);
      //  var geoStr = $"{minLon},{minLat}, {maxLon}, {maxLat}";
         var result = RasterHelper.TileXYToBoundingBox2(tileX, tileY, level,256);

         var (minLon, minLat, maxLon, maxLat) = (result[0], result[1], result[2], result[3]);
             var geoStr = $"{minLon},{minLat}, {maxLon}, {maxLat}";

       

        //var (x, y) = RasterHelper.LatLonToOffsets(result[0], result[1], mapWidth, mapHeight);

            string fileName = @"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\WarImage.png";
        var gpConverter = new GeoToPixelConverter(maxLat, minLat, maxLon, minLon, mapWidth, mapHeight);
        var (x, y) = RasterHelper.ConvertToPixel(minLon, maxLat, level, 256);

        // Create a new image at the cropped size
        Bitmap cropped = new Bitmap(256, 256);

        //Load image from file
        using (Image image = Image.FromFile(fileName))
        {
            // Create a Graphics object to do the drawing, *with the new bitmap as the target*
            using (Graphics g = Graphics.FromImage(cropped))         {

                // Draw the desired area of the original into the graphics object
                g.DrawImage(image, new Rectangle(0,0, 256, 256), new Rectangle(x, y, 256, 256), GraphicsUnit.Pixel);
                fileName = $@"D:\Everbridge\Story\VCC-6608-IHS Markit\ImageDumpFull\{quadKey}.png";
                // Save the result
                cropped.Save(fileName);
            }
        }


    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    public static (double x, double y) LatLonToOffsets(double latitude, double longitude, double mapWidth, double mapHeight)
    {
        const double FE = 180; // false easting
        double radius = mapWidth / (2 * Math.PI);

        double latRad = DegreesToRadians(latitude);
        double lonRad = DegreesToRadians(longitude + FE);

        // X coordinate (longitude to x)
        double x = lonRad * radius;

        // Y coordinate (latitude to y)
        double yFromEquator = radius * Math.Log(Math.Tan(Math.PI / 4 + latRad / 2));
        double y = mapHeight / 2 - yFromEquator;

        return (x, y);
    }

}
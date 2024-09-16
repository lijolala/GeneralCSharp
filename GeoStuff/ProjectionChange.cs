using System;
using System.IO;

using BitMiracle.LibTiff.Classic;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; ;
        string outputFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif";

        // Open the input GeoTIFF
        using (Tiff inputImage = Tiff.Open(filePath, "r"))
        {
            if (inputImage == null)
            {
                Console.WriteLine("Could not open input GeoTIFF.");
                return;
            }

            // Create a new output GeoTIFF with Web Mercator projection
            using (Tiff outputImage = Tiff.Open(outputFilePath, "w"))
            {
                if (outputImage == null)
                {
                    Console.WriteLine("Could not create output GeoTIFF.");
                    return;
                }

                TiffTag[] tagsToCopy = new TiffTag[]
                {
                    TiffTag.IMAGEWIDTH,
                    TiffTag.IMAGELENGTH,
                    TiffTag.BITSPERSAMPLE,
                    TiffTag.COMPRESSION,
                    TiffTag.PHOTOMETRIC,
                    TiffTag.STRIPOFFSETS,
                    TiffTag.ROWSPERSTRIP,
                    TiffTag.STRIPBYTECOUNTS,
                    TiffTag.XRESOLUTION,
                    TiffTag.YRESOLUTION,
                    TiffTag.RESOLUTIONUNIT,
                    TiffTag.GEOTIFF_MODELTIEPOINTTAG,    // Georeference tags
                    TiffTag.GEOTIFF_MODELPIXELSCALETAG,
                    TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG,
                    TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG
                };

                foreach (TiffTag tag in tagsToCopy)
                {
                    var fields = inputImage.GetField(tag);
                    if (fields != null)
                    {
                        outputImage.SetField(tag);  // Copy each tag to the new TIFF
                    }
                }
                // Update GeoTIFF metadata to Web Mercator (EPSG:3857)

                // Set the model pixel scale tag (units in meters for Web Mercator)
                double[] modelPixelScale = { 156543.03392804097, 156543.03392804097, 0.0 }; // for Web Mercator
                outputImage.SetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG, modelPixelScale);

                // Set the tie points (mapping raster to model coordinates)
                double[] modelTiepoint = { 0, 0, 0, -20037508.34, 20037508.34, 0 }; // Web Mercator origin
                outputImage.SetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG, modelTiepoint);

                // Set GeoKeyDirectoryTag, setting EPSG:3857
                // Web Mercator is EPSG:3857, but needs to be written with the appropriate GeoKeys
                int[] geoKeyDirectory = {
                    1, 1, 0, 4,        // Version info, number of keys
                    1024, 0, 1, 1,      // GTModelTypeGeoKey (1024) -> Projected Coordinate System (1)
                    1025, 0, 1, 3857,   // GTRasterTypeGeoKey (1025) -> EPSG:3857 (Web Mercator)
                    1026, 0, 1, 3857    // GeographicTypeGeoKey (1026) -> EPSG:3857 (Web Mercator)
                };
                outputImage.SetField(TiffTag.GEOTIFF_GEOKEYDIRECTORYTAG, geoKeyDirectory.Length, geoKeyDirectory);

                // Copy image data from input to output
                int numberOfStrips = inputImage.NumberOfStrips();
                for (int i = 0; i < numberOfStrips; i++)
                {
                    byte[] strip = new byte[inputImage.StripSize()];
                    inputImage.ReadRawStrip(i, strip, 0, strip.Length);
                    outputImage.WriteRawStrip(i, strip, strip.Length);
                }

                // Finalize and close
                outputImage.WriteDirectory();
                outputImage.Close();
            }

            inputImage.Close();
        }
    }
}

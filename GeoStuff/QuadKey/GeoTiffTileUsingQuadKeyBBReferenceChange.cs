using System;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using GeoAPI.CoordinateSystems;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace GeoTiffQuadKeyExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            // Example usage: pass the Quadkey and zoom level
            string quadkey = "120"; // Example quadkey
            string geoTiffFilePath = @"D:\Everbridge\Story\VCC-6608-IHS Markit\TiffDump\war_2023-08-19.tif"; ; // Path to your GeoTIFF file

            // Step 1: Convert Quadkey to tile coordinates and zoom level
            var (tileX, tileY, zoomLevel) = QuadkeyToTile(quadkey);

            // Step 2: Convert tile coordinates to bounding box in Web Mercator
            var (minLon, minLat, maxLon, maxLat) = TileToBoundingBox(tileX, tileY, zoomLevel);

            // Step 3: Reproject the bounding box to match the GeoTIFF's CRS (assuming WGS84 for now)
            var (minX, minY, maxX, maxY) = ReprojectBoundingBox(minLon, minLat, maxLon, maxLat, "EPSG:4326", "EPSG:4326");

            // Step 4: Extract the corresponding region from the GeoTIFF using libtiff
            ExtractRegionFromGeoTiff(geoTiffFilePath, minX, minY, maxX, maxY);

            Console.WriteLine("GeoTIFF region extracted based on Quadkey!");
        }

        // Step 1: Decode Quadkey to tile coordinates and zoom level
        public static (int tileX, int tileY, int zoomLevel) QuadkeyToTile(string quadkey)
        {
            int tileX = 0, tileY = 0;
            int zoomLevel = quadkey.Length;

            for (int i = 0; i < zoomLevel; i++)
            {
                int mask = 1 << (zoomLevel - 1 - i);
                switch (quadkey[i])
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
                        throw new ArgumentException("Invalid Quadkey digit.");
                }
            }
            return (tileX, tileY, zoomLevel);
        }

        // Step 2: Convert tile coordinates to bounding box in Web Mercator
        public static (double minLon, double minLat, double maxLon, double maxLat) TileToBoundingBox(int tileX, int tileY, int zoomLevel)
        {
            double n = Math.Pow(2.0, zoomLevel);

            double lon1 = tileX / n * 360.0 - 180.0;
            double lon2 = (tileX + 1) / n * 360.0 - 180.0;

            double lat1 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n))) * 180.0 / Math.PI;
            double lat2 = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (tileY + 1) / n))) * 180.0 / Math.PI;

            return (lon1, lat1, lon2, lat2);
        }

        // Step 3: Reproject the bounding box to match the GeoTIFF's CRS (using Proj.NET)
        public static (double minX, double minY, double maxX, double maxY) ReprojectBoundingBox(
            double minLon, double minLat, double maxLon, double maxLat, string sourceEPSG, string targetEPSG)
        {
            var sourceCS = GeographicCoordinateSystem.WGS84;
            var targetCS = ProjectedCoordinateSystem.;

            var transformationFactory = new CoordinateTransformationFactory();
            var transform = transformationFactory.CreateFromCoordinateSystems(sourceCS, targetCS);

            double[] minPoint = { minLon, minLat };
            double[] maxPoint = { maxLon, maxLat };

            minPoint = transform.MathTransform.Transform(minPoint);
            maxPoint = transform.MathTransform.Transform(maxPoint);

            return (minPoint[0], minPoint[1], maxPoint[0], maxPoint[1]);
        }
        // Method to create the Web Mercator (EPSG:3857) coordinate system
        public static IProjectedCoordinateSystem CreateWebMercatorCoordinateSystem()
        {
            // Define Web Mercator parameters (EPSG:3857)
            var parameters = new ProjectionParameter[]
            {
                new ProjectionParameter("semi_major", 6378137.0),  // Earth's radius (meters)
                new ProjectionParameter("semi_minor", 6378137.0),  // Earth's radius (meters, same as semi_major for sphere)
                new ProjectionParameter("latitude_of_origin", 0.0),
                new ProjectionParameter("central_meridian", 0.0),
                new ProjectionParameter("false_easting", 0.0),
                new ProjectionParameter("false_northing", 0.0),
                new ProjectionParameter("scale_factor", 1.0)
            };


            var projection = new Projection(
                "Mercator_1SP",
                parameters,
                "Mercator_1SP",
                "EPSG",
                3857
            );

            // Define the Web Mercator Coordinate System with EPSG:3857 parameters
            var geographicCS = GeographicCoordinateSystem.WGS84;
            var linearUnit = LinearUnit.Metre;

            var webMercatorCS = new ProjectedCoordinateSystem(
                "WGS 84 / Pseudo-Mercator",
                geographicCS,
                projection,
                linearUnit,
                new AxisInfo("East", AxisOrientationEnum.East),
                new AxisInfo("North", AxisOrientationEnum.North)
            );

            return webMercatorCS;
        }
        // Step 4: Extract the corresponding region from the GeoTIFF using libtiff
        public static void ExtractRegionFromGeoTiff(string tiffFilePath, double minX, double minY, double maxX, double maxY)
        {
            using (Tiff tiff = Tiff.Open(tiffFilePath, "r"))
            {
                // Read GeoTIFF metadata (PixelScale and Tiepoint)
                double[] modelPixelScale = new double[3];
                tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALE, out modelPixelScale);

                double[] modelTiepoint = new double[6];
                tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINT, out modelTiepoint);

                double scaleX = modelPixelScale[0];
                double scaleY = modelPixelScale[1];
                double originX = modelTiepoint[3];
                double originY = modelTiepoint[4];

                // Calculate pixel coordinates from the bounding box
                int startX = (int)((minX - originX) / scaleX);
                int endX = (int)((maxX - originX) / scaleX);
                int startY = (int)((originY - maxY) / scaleY);
                int endY = (int)((originY - minY) / scaleY);

                int width = endX - startX;
                int height = endY - startY;

                byte[] buffer = new byte[width * height * 3]; // Assuming RGB image

                for (int y = startY; y < endY; y++)
                {
                    tiff.ReadScanline(buffer, y * width, startX);
                }

                // The extracted region is now in 'buffer'. You can save it as a new image or process it further.
                Console.WriteLine("Extracted region saved in buffer.");
            }
        }
    }
}

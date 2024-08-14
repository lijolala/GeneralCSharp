using IDV.VCC.Data.Spatial;

using System;

namespace Diagnostics
{
    public class SqlSpatial
    {
        public static void Main(string[] args)
        {
            bool _isSqlTypesLoaded = false;
            if (!_isSqlTypesLoaded)
            {
                SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
                _isSqlTypesLoaded = true;
            }
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            var test = new Geometry();
            Console.WriteLine("test done");
            const int WorldGeodeticSystemId = 4326;

        //    var firstlocationLonLat = new Tuple<string, string>("-0.081389", "51.502195");
        //    var secondlocationLonLat = new Tuple<string, string>("-0.185348", "51.410933");

        //    var firstLocationAsPoint = string.Format("POINT({0} {1})", firstlocationLonLat.Item1, firstlocationLonLat.Item2);
        //    var secondLocationAsPoint = string.Format("POINT({0} {1})", secondlocationLonLat.Item1, secondlocationLonLat.Item2);

        //    var firstLocation = SqlGeography.STGeomFromText(new SqlChars(firstLocationAsPoint), WorldGeodeticSystemId);
        //    var secondLocation = SqlGeography.STGeomFromText(new SqlChars(secondLocationAsPoint), WorldGeodeticSystemId);
        ////    var test = 

        //    var distance = firstLocation.STDistance(secondLocation);

        //    Console.WriteLine("First Location is " + MetersToMiles((double)distance).ToString("0") + " miles from Second Location");
            Console.ReadKey();
        }

        public static double MetersToMiles(double? meters)
        {
            if (meters == null)
            {
                return 0F;
            }

            return meters.Value * 0.000621371192;
        }    
    }
}

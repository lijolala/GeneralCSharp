using System;

class Program
{
    static void Main()
    {
        double mapWidth = 10095;
        double mapHeight = 5047;

        double mapLonLeft = 9.8;
        double mapLonRight = 10.2;
        double mapLonDelta = mapLonRight - mapLonLeft;

        double mapLatBottom = 53.45;
        double mapLatBottomDegree = mapLatBottom * Math.PI / 180;

        // Latitude and longitude to convert
        double lat = 41.85;
        double lon = -87.65;


        //var PI = Math.PI;
        //var x = (lon + 180) * (mapWidth / 360);
        //// convert from degrees to radians
        //var latRad = lat * PI / 180;
        //// get y value
        //var mercN = Math.Log(Math.Tan(PI / 4) + (latRad / 2));
        //var y = (mapHeight / 2) - (mapWidth * mercN / (2 * PI));

        // Mercator projection constants
        var PI = Math.PI;

        // Convert longitude (lon) to X coordinate
        // Normalize the longitude between the map bounds
        double x = (lon - mapLonLeft) * (mapWidth / mapLonDelta);

        // Convert latitude to radians
        double latRad = lat * PI / 180;

        // Mercator formula for latitude to Y conversion
        double mercN = Math.Log(Math.Tan((PI / 4) + (latRad / 2)));
        double y = (mapHeight / 2) - (mapWidth * mercN / (2 * PI));


        // var pixelPosition = ConvertGeoToPixel(lat, lon, mapWidth, mapHeight, mapLonLeft, mapLonDelta, mapLatBottomDegree);
        //  Console.WriteLine($"x: {pixelPosition.Item1}, y: {pixelPosition.Item2}");
    }

    //static Tuple<double, double> ConvertGeoToPixel(double lat, double lon, double mapWidth, double mapHeight, double mapLonLeft, double mapLonDelta, double mapLatBottomDegree)
    //{
    //    // Convert longitude to x coordinate
    //    double x = (lon - mapLonLeft) * (mapWidth / mapLonDelta);

    //    // Convert latitude to y coordinate
    //    lat = lat * Math.PI / 180;
    //    double worldMapWidth = ((mapWidth / mapLonDelta) * 360) / (2 * Math.PI);
    //    double mapOffsetY = (worldMapWidth / 2 * Math.Log((1 + Math.Sin(mapLatBottomDegree)) / (1 - Math.Sin(mapLatBottomDegree))));
    //    double y = mapHeight - ((worldMapWidth / 2 * Math.Log((1 + Math.Sin(lat)) / (1 - Math.Sin(lat)))) - mapOffsetY);

    //    return Tuple.Create(x, y);
    //}

    static Point ConvertGeoToPixel(
        double latitude, double longitude, // The coordinate to translate
        double imageWidth, double imageHeight, // The dimensions of the target space (in pixels)
        double mapLonLeft, double mapLonRight, double mapLatBottom // The bounds of the target space (in geo coordinates)
    )
    {
        double mapLatBottomRad = mapLatBottom * Math.PI / 180;
        double latitudeRad = latitude * Math.PI / 180;

        double mapLonDelta = mapLonRight - mapLonLeft;
        double worldMapWidth = (imageWidth / mapLonDelta * 360) / (2 * Math.PI);
        double mapOffsetY = worldMapWidth / 2 * Math.Log((1 + Math.Sin(mapLatBottomRad)) / (1 - Math.Sin(mapLatBottomRad)));

        double x = (longitude - mapLonLeft) * (imageWidth / mapLonDelta);
        double y = imageHeight - ((worldMapWidth / 2 * Math.Log((1 + Math.Sin(latitudeRad)) / (1 - Math.Sin(latitudeRad)))) - mapOffsetY);

        return new Point()
        {
            X = Convert.ToInt32(x),
            Y = Convert.ToInt32(y)
        };
    }
}
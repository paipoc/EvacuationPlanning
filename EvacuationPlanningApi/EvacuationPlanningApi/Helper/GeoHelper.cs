namespace EvacuationPlanningApi.Helper
{
    public static class GeoHelper
    {
        // Haversine Formula https://stormconsultancy.co.uk/blog/storm-news/the-haversine-formula-in-c-and-sql/
        // https://en.wikipedia.org/wiki/Haversine_formula
        public static double CalculateDistance(double pos1Latitude, double pos1Longitude, double pos2Latitude, double pos2Longitude)
        {
            double R = 6371;
            var lat = ToRadians(pos2Latitude - pos1Latitude);
            var lng = ToRadians(pos2Longitude - pos1Longitude);
            var h1 = Math.Sin(lat / 2) * Math.Sin(lat / 2) +
                          Math.Cos(ToRadians(pos1Latitude)) * Math.Cos(ToRadians(pos2Latitude)) *
                          Math.Sin(lng / 2) * Math.Sin(lng / 2);
            var h2 = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h1)));
            return R * h2;
        }
        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public static double CalculateETA(double distanceKm, double speedKmPerHour)
        {
            if (speedKmPerHour <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speedKmPerHour), "Speed must be greater than 0.");
            }

            return (distanceKm / speedKmPerHour) * 60.0;
        }
    }
}

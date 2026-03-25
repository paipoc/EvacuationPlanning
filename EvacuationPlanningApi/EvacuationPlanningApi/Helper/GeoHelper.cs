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

        public static string CalculateETA(double distanceKm, double speedKmPerHour)
        {
            if (speedKmPerHour <= 0) return "0 minutes";

            // Calculate total minutes
            double totalMinutes = (distanceKm / speedKmPerHour) * 60.0;

            // Convert to a TimeSpan object
            TimeSpan time = TimeSpan.FromMinutes(totalMinutes);

            // Build the string
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours} hour {time.Minutes} minutes";
            }

            return $"{time.Minutes} minutes";
        }
    }
}

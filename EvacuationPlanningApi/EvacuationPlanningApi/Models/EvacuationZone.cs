namespace EvacuationPlanningApi.Models
{
    public class EvacuationZone
    {
        public string ZoneId { get; set; } = default!;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int NumberOfPeople { get; set; }

        public UrgencyLevel UrgencyLevel { get; set; }
    }
    public enum UrgencyLevel
    {
        Low = 1,
        BelowNormal = 2,
        Medium = 3,
        High = 4,
        Critical = 5
    }
}

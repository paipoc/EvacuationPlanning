using System.Text.Json.Serialization;

namespace EvacuationPlanningApi.Models
{
    public class EvacuationZoneTrackingStatus
    {
        public string TrackingId { get; set; } = Guid.NewGuid().ToString("N");
        public string ZoneId { get; set; } = default!;
        public int TotalEvacuated { get; set; }
        public int RemainingPeople { get; set; }
        public string? VehicleUsed { get; set; }
        public ZoneTrackingStatus Status { get; set; }
        public DateTime DateAdd { get; set; } = DateTime.UtcNow;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ZoneTrackingStatus
        {
            Uncomplete = 1,
            Complete = 2,
        }
    }
}

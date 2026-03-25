namespace EvacuationPlanningApi.Models
{
    public class EvacuationZoneStatus
    {
        public string ZoneId { get; set; } = default!;
        public int TotalEvacuated { get; set; }
        public int RemainingPeople { get; set; }
        public string? LastVehicleUsed { get; set; }
    }
}

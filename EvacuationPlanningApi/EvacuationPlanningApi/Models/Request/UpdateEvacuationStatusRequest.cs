namespace EvacuationPlanningApi.Models
{
    public class UpdateEvacuationStatusRequest
    {
        public string ZoneId { get; set; }
        public string VehicleId { get; set; }
        public int EvacueesMoved { get; set; }
    }
}

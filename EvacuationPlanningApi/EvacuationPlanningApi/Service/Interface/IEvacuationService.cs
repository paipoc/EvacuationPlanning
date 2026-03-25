using EvacuationPlanningApi.Models;

namespace EvacuationPlanningApi.Service.Interface
{
    public interface IEvacuationService
    {
        Task AddZoneAsync(EvacuationZone request);
        Task AddVehicleAsync(Vehicle vehicleModel);
        Task<List<EvacuationPlan>> GeneratePlanAsync();
        Task<IReadOnlyList<EvacuationZoneStatus>> GetEvacuationZoneStatusAsync();
        Task UpdateEvacuationAsync(UpdateEvacuationStatusRequest request);
        Task<IReadOnlyList<EvacuationZoneTrackingStatus>> GetEvacuationZoneTrackingStatusAsync(string zoneId);
        Task ClearAllAsync();
        Task SeedDataAsync();
    }
}

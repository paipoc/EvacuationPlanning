using EvacuationPlanningApi.Models;

namespace EvacuationPlanningApi.Cache
{
    public interface IEvacuationRepository
    {
        Task<EvacuationZoneStatus?> GetStatusAsync(string zoneId);
        Task SetStatusAsync(EvacuationZoneStatus status, TimeSpan? expiry = null);
        Task RemoveStatusAsync(string zoneId);

        Task<EvacuationZoneTrackingStatus?> GetTrackingAsync(string zoneId, string trackingId);
        Task SetTrackingAsync(EvacuationZoneTrackingStatus tracking, TimeSpan? expiry = null);
        Task RemoveTrackingAsync(string zoneId, string trackingId);

    }
}

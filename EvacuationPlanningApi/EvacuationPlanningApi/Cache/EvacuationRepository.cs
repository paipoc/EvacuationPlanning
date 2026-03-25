using EvacuationPlanningApi.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EvacuationPlanningApi.Cache
{

    public class EvacuationRepository : IEvacuationRepository
    {
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromHours(1);

        public EvacuationRepository(IDistributedCache cache)
        {
            _cache = cache;
        }

        private static string GetStatusKey(string zoneId) => $"evac:status:{zoneId}";
        private static string GetTrackingKey(string zoneId, string trackingId) => $"evac:tracking:{zoneId}:{trackingId}";

        public async Task<EvacuationZoneStatus?> GetStatusAsync(string zoneId)
        {
            var json = await _cache.GetStringAsync(GetStatusKey(zoneId));

            return json == null
                ? null
                : JsonSerializer.Deserialize<EvacuationZoneStatus>(json);
        }

        public async Task SetStatusAsync(EvacuationZoneStatus status, TimeSpan? expiry = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
            };

            var json = JsonSerializer.Serialize(status);
            await _cache.SetStringAsync(GetStatusKey(status.ZoneId), json, options);
        }

        public async Task RemoveStatusAsync(string zoneId)
        {
            await _cache.RemoveAsync(GetStatusKey(zoneId));
        }

        public async Task<EvacuationZoneTrackingStatus?> GetTrackingAsync(string zoneId, string trackingId)
        {
            var json = await _cache.GetStringAsync(GetTrackingKey(zoneId, trackingId));

            return json == null
                ? null
                : JsonSerializer.Deserialize<EvacuationZoneTrackingStatus>(json);
        }

        public async Task SetTrackingAsync(EvacuationZoneTrackingStatus tracking, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(tracking.TrackingId))
            {
                tracking.TrackingId = Guid.NewGuid().ToString();
            }

            if (tracking.DateAdd == default)
            {
                tracking.DateAdd = DateTime.UtcNow;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
            };

            var json = JsonSerializer.Serialize(tracking);
            await _cache.SetStringAsync(GetTrackingKey(tracking.ZoneId, tracking.TrackingId), json, options);
        }

        public async Task RemoveTrackingAsync(string zoneId, string trackingId)
        {
            await _cache.RemoveAsync(GetTrackingKey(zoneId, trackingId));
        }
    }
}
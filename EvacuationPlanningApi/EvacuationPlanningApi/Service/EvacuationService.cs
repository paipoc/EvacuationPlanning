using EvacuationPlanningApi.Cache;
using EvacuationPlanningApi.Helper;
using EvacuationPlanningApi.Models;
using EvacuationPlanningApi.Service.Interface;
namespace EvacuationPlanningApi.Service
{
    public class EvacuationService : IEvacuationService
    {
        private readonly ILogger<EvacuationService> _logger;
        private readonly IEvacuationRepository _repository;

        private readonly List<EvacuationZone> evacuationZones = new();
        private readonly List<Vehicle> vehicles = new();
        private readonly List<EvacuationPlan> evacuationPlans = new();
        private readonly List<(string ZoneId, string ZoneTrackingStatusId)> zoneTrackingStatuses = new();

        public EvacuationService(ILogger<EvacuationService> logger, IEvacuationRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }
        public async Task SeedDataAsync()
        {
            evacuationZones.Add(new EvacuationZone() { ZoneId = "Z1", Latitude = 13.7563, Longitude = 100.1018, NumberOfPeople = 100, UrgencyLevel = UrgencyLevel.High });
            var zoneStatus1 = new EvacuationZoneStatus
            {
                ZoneId = "Z1",
                TotalEvacuated = 0,
                RemainingPeople = 100,
                LastVehicleUsed = null
            };
            _repository.SetStatusAsync(zoneStatus1);

            evacuationZones.Add(new EvacuationZone() { ZoneId = "Z2", Latitude = 13.7367, Longitude = 100.0231, NumberOfPeople = 50, UrgencyLevel = UrgencyLevel.Critical });
            var zoneStatus2 = new EvacuationZoneStatus
            {
                ZoneId = "Z2",
                TotalEvacuated = 0,
                RemainingPeople = 50,
                LastVehicleUsed = null
            };
            _repository.SetStatusAsync(zoneStatus2);
            vehicles.Add(new Vehicle() { VehicleId = "V1", Capacity = 40, Type = VehicleType.Bus, Latitude = 13.7650, Longitude = 100.5381, Speed = 60 });
            vehicles.Add(new Vehicle() { VehicleId = "V2", Capacity = 20, Type = VehicleType.Bus, Latitude = 13.7320, Longitude = 100.5200, Speed = 50 });
            vehicles.Add(new Vehicle() { VehicleId = "V3", Capacity = 20, Type = VehicleType.Bus, Latitude = 13.7320, Longitude = 100.5200, Speed = 50 });
        }
        public async Task AddZoneAsync(EvacuationZone evacuationZoneModel)
        {
            if (!Enum.IsDefined(typeof(UrgencyLevel), evacuationZoneModel.UrgencyLevel))
            {
                throw new ArgumentException("UrgencyLevel must be between 1 and 5.");
            }

            if (evacuationZones.Any(z => z.ZoneId.Equals(evacuationZoneModel.ZoneId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Zone '{evacuationZoneModel.ZoneId}' already exists.");
            }

            var zone = new EvacuationZone
            {
                ZoneId = evacuationZoneModel.ZoneId,
                Latitude = evacuationZoneModel.Latitude,
                Longitude = evacuationZoneModel.Longitude,
                NumberOfPeople = evacuationZoneModel.NumberOfPeople,
                UrgencyLevel = evacuationZoneModel.UrgencyLevel
            };

            evacuationZones.Add(zone);

            var evacuationZoneStatus = new EvacuationZoneStatus
            {
                ZoneId = zone.ZoneId,
                TotalEvacuated = 0,
                RemainingPeople = zone.NumberOfPeople,
                LastVehicleUsed = null,
            };
            _repository.SetStatusAsync(evacuationZoneStatus);

            _logger.LogInformation(
                "Evacuation zone created. ZoneId={ZoneId}, People={People}, UrgencyLevel={UrgencyLevel}",
                zone.ZoneId,
                zone.NumberOfPeople,
                zone.UrgencyLevel);

        }

        public async Task AddVehicleAsync(Vehicle vehicleModel)
        {

            var vehicle = new Vehicle
            {
                VehicleId = vehicleModel.VehicleId,
                Capacity = vehicleModel.Capacity,
                Type = vehicleModel.Type,
                Latitude = vehicleModel.Latitude,
                Longitude = vehicleModel.Longitude,
                Speed = vehicleModel.Speed
            };

            vehicles.Add(vehicle);

            _logger.LogInformation(
                "Vehicle registered. VehicleId={VehicleId}, Type={Type}, Capacity={Capacity}, Speed={Speed}",
                vehicle.VehicleId,
                vehicle.Type,
                vehicle.Capacity,
                vehicle.Speed);

        }

        public async Task<List<EvacuationPlan>> GeneratePlanAsync()
        {
            if (evacuationZones.Count == 0)
            {
                throw new InvalidOperationException("No evacuation zones available.");
            }

            if (vehicles.Count == 0)
            {
                throw new InvalidOperationException("No vehicles available.");
            }

            evacuationPlans.Clear();

            var usedVehicleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Order evacuation zone by urgency level first then remaining people
            var prioritizedZoneTasks = evacuationZones.Select(async zone =>
            {
                var zoneStatus = await _repository.GetStatusAsync(zone.ZoneId).ConfigureAwait(false);
                var remainingPeople = zoneStatus?.RemainingPeople ?? zone.NumberOfPeople;

                return new
                {
                    Zone = zone,
                    RemainingPeople = remainingPeople
                };
            });

            var prioritizedZoneResults = await Task.WhenAll(prioritizedZoneTasks).ConfigureAwait(false);

            var prioritizedZones = prioritizedZoneResults
                .Where(x => x.RemainingPeople > 0)
                .OrderByDescending(x => x.Zone.UrgencyLevel)
                .ThenByDescending(x => x.RemainingPeople)
                .ToList();

            foreach (var zoneItem in prioritizedZones)
            {
                var zone = zoneItem.Zone;
                var remainingPeople = zoneItem.RemainingPeople;

                // Order vehicle by closest distance first then vehicle larger capacity.
                // Vehicles must be within a reasonable distance.
                const double maxReasonableDistanceKm = 200;
                var candidateVehicles = vehicles
                    .Where(v => !usedVehicleIds.Contains(v.VehicleId))
                    .Select(v =>
                    {
                        var distanceKm = GeoHelper.CalculateDistance(
                            v.Latitude,
                            v.Longitude,
                            zone.Latitude,
                            zone.Longitude);

                        var etaMinutes = GeoHelper.CalculateETA(distanceKm, v.Speed);

                        return new
                        {
                            Vehicle = v,
                            DistanceKm = distanceKm,
                            EtaMinutes = etaMinutes
                        };
                    })
                    .Where(x => x.DistanceKm <= maxReasonableDistanceKm)
                    .OrderBy(x => x.DistanceKm)
                    .ThenByDescending(x => x.Vehicle.Capacity)
                    .ToList();

                if (!candidateVehicles.Any())
                {
                    _logger.LogWarning(
                        "No available vehicles within reasonable distance. Or no vehicle available ZoneId={ZoneId}",
                        zone.ZoneId);

                    continue;
                }

                foreach (var candidate in candidateVehicles)
                {

                    // in case of remaining people less than vehicle capacity
                    var peopleToEvacuate = Math.Min(candidate.Vehicle.Capacity, remainingPeople);

                    evacuationPlans.Add(new EvacuationPlan
                    {
                        ZoneId = zone.ZoneId,
                        VehicleId = candidate.Vehicle.VehicleId,
                        ETA = candidate.EtaMinutes,
                        NumberOfPeople = peopleToEvacuate
                    });

                    // Add vehicle to in-use list.
                    usedVehicleIds.Add(candidate.Vehicle.VehicleId);


                }
            }

            return evacuationPlans;
        }

        public async Task<IReadOnlyList<EvacuationZoneStatus>> GetEvacuationZoneStatusAsync()
        {

            var statusTasks = evacuationZones
                .Select(zone => _repository.GetStatusAsync(zone.ZoneId));

            var statuses = await Task.WhenAll(statusTasks).ConfigureAwait(false);

            return statuses.ToList().AsReadOnly();
        }

        public async Task UpdateEvacuationAsync(UpdateEvacuationStatusRequest request)
        {
            var zoneStatus = await _repository.GetStatusAsync(request.ZoneId).ConfigureAwait(false);
            var zoneTrackingStatusId = Guid.NewGuid().ToString();

            if (zoneStatus.RemainingPeople == 0)
            {
                throw new InvalidOperationException("No remaining people.");
            }
            if ( request.EvacueesMoved> zoneStatus.RemainingPeople)
            {
                throw new InvalidOperationException("Evacuees moved exceed remaining people.");
            }

            // Add zone tracking status on redis
            await _repository.SetTrackingAsync(new EvacuationZoneTrackingStatus
            {
                TrackingId = zoneTrackingStatusId,
                ZoneId = request.ZoneId,
                TotalEvacuated = request.EvacueesMoved,
                RemainingPeople = Math.Max(0, zoneStatus.RemainingPeople - request.EvacueesMoved),
                VehicleUsed = request.VehicleId,
                Status = EvacuationZoneTrackingStatus.ZoneTrackingStatus.Uncomplete,

            }).ConfigureAwait(false);

            // Add zone tracking status on redis
            zoneTrackingStatuses.Add((request.ZoneId, zoneTrackingStatusId));

            // update zone status on redis
            await _repository.SetStatusAsync(new EvacuationZoneStatus
            {
                ZoneId = request.ZoneId,
                TotalEvacuated = request.EvacueesMoved,
                RemainingPeople = Math.Max(0, zoneStatus.RemainingPeople - request.EvacueesMoved),
                LastVehicleUsed = request.VehicleId,
            }).ConfigureAwait(false);

        }

        public async Task<IReadOnlyList<EvacuationZoneTrackingStatus>> GetEvacuationZoneTrackingStatusAsync(string zoneId)
        {

            var zoneTrackingStatusTasks = zoneTrackingStatuses
                .Select(zoneTrackingStatus => _repository.GetTrackingAsync(zoneTrackingStatus.ZoneId, zoneTrackingStatus.ZoneTrackingStatusId));

            var statuses = await Task.WhenAll(zoneTrackingStatusTasks).ConfigureAwait(false);

            return statuses.ToList().AsReadOnly();
        }

        public async Task ClearAllAsync()
        {
            foreach (var zone in evacuationZones)
            {
                await _repository.RemoveStatusAsync(zone.ZoneId).ConfigureAwait(false);
            }
            foreach (var zoneTrackingStatus in zoneTrackingStatuses)
            {
                await _repository.RemoveTrackingAsync(zoneTrackingStatus.ZoneId, zoneTrackingStatus.ZoneTrackingStatusId).ConfigureAwait(false);
            }
            evacuationZones.Clear();
            vehicles.Clear();

            _logger.LogWarning("All evacuation data cleared.");
        }
    }
}

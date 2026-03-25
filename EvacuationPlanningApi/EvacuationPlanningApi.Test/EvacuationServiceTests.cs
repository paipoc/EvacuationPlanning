using EvacuationPlanningApi.Cache;
using EvacuationPlanningApi.Models;
using EvacuationPlanningApi.Service;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace EvacuationPlanningApi.Test
{
    public class EvacuationServiceTests
    {
        private readonly IEvacuationRepository _repository;
        private readonly ILogger<EvacuationService> _logger;
        private readonly EvacuationService _sut;

        public EvacuationServiceTests()
        {
            _repository = Substitute.For<IEvacuationRepository>();
            _logger = Substitute.For<ILogger<EvacuationService>>();

            SetupZoneStatus("Z1", remaining: 100);
            SetupZoneStatus("Z2", remaining: 50);

            _sut = new EvacuationService(_logger, _repository);
        }

        private void SetupZoneStatus(string zoneId, int remaining, int totalEvacuated = 0, string? lastVehicle = null)
        {
            _repository.GetStatusAsync(zoneId).Returns(new EvacuationZoneStatus
            {
                ZoneId = zoneId,
                TotalEvacuated = totalEvacuated,
                RemainingPeople = remaining,
                LastVehicleUsed = lastVehicle
            });
        }

        #region AddZoneAsync

        [Fact]
        public async Task AddZoneAsync_ValidZone_SetsInitialStatusInRepository()
        {
            var zone = new EvacuationZone
            {
                ZoneId = "Z3",
                Latitude = 13.7400,
                Longitude = 100.5100,
                NumberOfPeople = 30,
                UrgencyLevel = UrgencyLevel.Medium
            };

            await _sut.AddZoneAsync(zone);

            await _repository.Received(1).SetStatusAsync(
                Arg.Is<EvacuationZoneStatus>(s =>
                    s.ZoneId == "Z3" &&
                    s.RemainingPeople == 30 &&
                    s.TotalEvacuated == 0 &&
                    s.LastVehicleUsed == null),
                Arg.Any<TimeSpan?>());
        }

        [Fact]
        public async Task AddZoneAsync_DuplicateZoneId_ThrowsInvalidOperationException()
        {
            var duplicate = new EvacuationZone
            {
                ZoneId = "Z1", // already seeded in constructor
                Latitude = 13.7563,
                Longitude = 100.5018,
                NumberOfPeople = 20,
                UrgencyLevel = UrgencyLevel.Low
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddZoneAsync(duplicate));
        }


        #endregion

        #region AddVehicleAsync

        [Fact]
        public async Task AddVehicleAsync_ValidVehicle_DoesNotThrow()
        {
            var vehicle = new Vehicle
            {
                VehicleId = "V99",
                Capacity = 30,
                Type = VehicleType.Van,
                Latitude = 13.7500,
                Longitude = 100.5200,
                Speed = 80
            };

            var exception = await Record.ExceptionAsync(() => _sut.AddVehicleAsync(vehicle));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(VehicleType.Bus)]
        [InlineData(VehicleType.Van)]
        [InlineData(VehicleType.Boat)]
        public async Task AddVehicleAsync_AllVehicleTypes_DoesNotThrow(VehicleType type)
        {
            var vehicle = new Vehicle
            {
                VehicleId = $"V_{(int)type}",
                Capacity = 20,
                Type = type,
                Latitude = 13.7500,
                Longitude = 100.5200,
                Speed = 60
            };

            var exception = await Record.ExceptionAsync(() => _sut.AddVehicleAsync(vehicle));

            Assert.Null(exception);
        }

        #endregion

        #region GeneratePlanAsync

        [Fact]
        public async Task GeneratePlanAsync_WithSeededData_ReturnsNonEmptyPlan()
        {
            var plans = await _sut.GeneratePlanAsync();

            Assert.NotEmpty(plans);
        }

        [Fact]
        public async Task GeneratePlanAsync_CriticalZone_AssignedBeforeHighUrgencyZone()
        {
            // Z2 is Critical (5), Z1 is High (4) then Z2 must come first in the plan
            var plans = await _sut.GeneratePlanAsync();

            Assert.Equal("Z2", plans.First().ZoneId);
        }

        [Fact]
        public async Task GeneratePlanAsync_NumberOfPeople_NeverExceedsVehicleCapacity()
        {
            var plans = await _sut.GeneratePlanAsync();

            // Maximum seeded capacity is 40 (V1)
            Assert.All(plans, plan => Assert.True(plan.NumberOfPeople <= 40));
        }


        [Fact]
        public async Task GeneratePlanAsync_NoZonesAvailable_ThrowsInvalidOperationException()
        {
            await _sut.ClearAllAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GeneratePlanAsync());
        }


        [Fact]
        public async Task GeneratePlanAsync_ZoneWithZeroRemainingPeople_IsExcludedFromPlan()
        {
            // Set Z2 remaining to 0 so it is filtered out
            SetupZoneStatus("Z1", remaining: 100);
            SetupZoneStatus("Z2", remaining: 0);

            var plans = await _sut.GeneratePlanAsync();

            Assert.DoesNotContain(plans, p => p.ZoneId == "Z2");
        }

        #endregion

        #region GetEvacuationZoneStatusAsync

        [Fact]
        public async Task GetEvacuationZoneStatusAsync_ReturnsStatusForEverySeededZone()
        {
            var statuses = await _sut.GetEvacuationZoneStatusAsync();

            Assert.NotNull(statuses);
            Assert.Equal(2, statuses.Count); // Z1, Z2 seeded in constructor
        }

        [Fact]
        public async Task GetEvacuationZoneStatusAsync_Z1Status_ReturnsCorrectRemainingPeople()
        {
            var statuses = await _sut.GetEvacuationZoneStatusAsync();

            var z1 = statuses.FirstOrDefault(s => s?.ZoneId == "Z1");
            Assert.NotNull(z1);
            Assert.Equal(100, z1!.RemainingPeople);
        }


        #endregion

        #region UpdateEvacuationAsync

        [Fact]
        public async Task UpdateEvacuationAsync_ValidRequest_UpdatesZoneStatusInRepository()
        {
            SetupZoneStatus("Z1", remaining: 100);

            var request = new UpdateEvacuationStatusRequest
            {
                ZoneId = "Z1",
                VehicleId = "V1",
                EvacueesMoved = 40
            };

            await _sut.UpdateEvacuationAsync(request);

            await _repository.Received(1).SetStatusAsync(
                Arg.Is<EvacuationZoneStatus>(s =>
                    s.ZoneId == "Z1" &&
                    s.TotalEvacuated == 40 &&
                    s.RemainingPeople == 60 &&
                    s.LastVehicleUsed == "V1"),
                Arg.Any<TimeSpan?>());
        }

        #endregion

        #region ClearAllAsync

        [Fact]
        public async Task ClearAllAsync_RemovesStatusForEverySeededZone()
        {
            await _sut.ClearAllAsync();

            await _repository.Received(1).RemoveStatusAsync("Z1");
            await _repository.Received(1).RemoveStatusAsync("Z2");
        }

        #endregion
    }
}

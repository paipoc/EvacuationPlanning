using EvacuationPlanningApi.Models;
using EvacuationPlanningApi.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace EvacuationPlanningApi.Controllers
{
    [Route("api")]
    public class EvacuationApiController : ApiControllerBase
    {
        private readonly IEvacuationService evacuationService;

        public EvacuationApiController(IEvacuationService evacuationService)
        {
            this.evacuationService = evacuationService;
        }

        [HttpPost("evacuation-zones")]
        public async Task<IActionResult> AddZoneAsync([FromBody] EvacuationZone model)
        {
            await evacuationService.AddZoneAsync(model);
            return Ok();
        }

        [HttpPost("vehicles")]
        public async Task<IActionResult> AddVehicleAsync([FromBody] Vehicle model)
        {
            await evacuationService.AddVehicleAsync(model);
            return Ok();
        }

        [HttpPost("evacuations/plan")]
        public async Task<IActionResult> GeneratePlanAsync()
        {
            var evacuationPlans = await evacuationService.GeneratePlanAsync();
            return Ok(evacuationPlans);
        }

        [HttpGet("evacuations/status")]
        public async Task<IActionResult> GetEvacuationZoneStatusAsync()
        {
            var evacuationZoneStatuses = await evacuationService.GetEvacuationZoneStatusAsync();
            return Ok(evacuationZoneStatuses);
        }

        [HttpGet("evacuations/{zoneId}/tracking")]
        public async Task<IActionResult> GetTrackingHistoryAsync(string zoneId)
        {
            var history = await evacuationService.GetEvacuationZoneTrackingStatusAsync(zoneId);
            return Ok(history);
        }

        [HttpPost("evacuations/seeddata")]
        public async Task<IActionResult> SeedDataAsync()
        {
            await evacuationService.SeedDataAsync();
            
            return Ok();
        }

        [HttpPut("evacuations/update")]
        public async Task<IActionResult> UpdateEvacuationAsync([FromBody] UpdateEvacuationStatusRequest request) 
        {
            await evacuationService.UpdateEvacuationAsync(request);
            return Ok();
        }

        [HttpDelete("evacuations/clear")]
        public async Task<IActionResult> ClearAllAsync()
        {
            await evacuationService.ClearAllAsync();
            return Ok();
        }
    }
}

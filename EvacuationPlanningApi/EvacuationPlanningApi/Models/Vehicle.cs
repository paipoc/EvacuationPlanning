
using System.Text.Json.Serialization;

namespace EvacuationPlanningApi.Models
{
    public class Vehicle
    {
        public string VehicleId { get; set; } = default!;

        public int Capacity { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public VehicleType Type { get; set; } = default!;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        /// <summary>
        /// Vehicle average speed in km/h.
        /// </summary>
        public double Speed { get; set; }
    }
    public enum VehicleType
    {
        Bus = 1,
        Van = 2,
        Boat = 3
    }
}

using Microsoft.AspNetCore.Mvc;
using RideNowAPI.Services;

namespace RideNowAPI.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
    {
        private readonly LocationService _locationService;

        public LocationController(LocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpPost("calculate-fare")]
        public IActionResult CalculateFare([FromBody] FareCalculationDto dto)
        {
            var distance = _locationService.CalculateDistance(
                dto.PickupLatitude, dto.PickupLongitude,
                dto.DropLatitude, dto.DropLongitude);

            var fare = _locationService.CalculateFare(distance, dto.VehicleType);

            return Ok(new { distance = Math.Round(distance, 2), fare });
        }

        [HttpPost("calculate-distance")]
        public IActionResult CalculateDistance([FromBody] DistanceDto dto)
        {
            var distance = _locationService.CalculateDistance(
                dto.Lat1, dto.Lng1, dto.Lat2, dto.Lng2);

            return Ok(new { distance = Math.Round(distance, 2) });
        }
    }

    public class FareCalculationDto
    {
        public decimal PickupLatitude { get; set; }
        public decimal PickupLongitude { get; set; }
        public decimal DropLatitude { get; set; }
        public decimal DropLongitude { get; set; }
        public string VehicleType { get; set; } = string.Empty;
    }

    public class DistanceDto
    {
        public decimal Lat1 { get; set; }
        public decimal Lng1 { get; set; }
        public decimal Lat2 { get; set; }
        public decimal Lng2 { get; set; }
    }
}

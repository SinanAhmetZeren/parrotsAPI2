using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services;
using ParrotsAPI2.Dtos;
using ParrotsAPI2.Dtos.AiDtos;
using Microsoft.AspNetCore.Authorization;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }




        [HttpGet("weeklyPurchases")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyPurchaseDto>>>> GetWeeklyPurchases()
        {
            var response = await _metricsService.GetWeeklyPurchases();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("weeklyTransactions")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyTransactionsDto>>>> GetWeeklyTransactions()
        {
            var response = await _metricsService.GetWeeklyTransactions();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("weeklyVoyages")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVoyagesDto>>>> GetWeeklyVoyages()
        {
            var response = await _metricsService.GetWeeklyVoyagesCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpGet("weeklyVehicles")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVehiclesDto>>>> GetWeeklyVehicles()
        {
            var response = await _metricsService.GetWeeklyVehiclesCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("weeklyUsers")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVehiclesDto>>>> GetWeeklyUsers()
        {
            var response = await _metricsService.GetWeeklyUsersCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("weeklyBids")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyBidsDto>>>> GetWeeklyBids()
        {
            var response = await _metricsService.GetWeeklyBids();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpGet("weeklyMessages")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyMessagesDto>>>> GetWeeklyMessages()
        {
            var response = await _metricsService.GetWeeklyMessages();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("aiQueryStats")]
        public async Task<ActionResult<ServiceResponse<List<AiQueryDayDto>>>> GetAiQueryStats(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var response = await _metricsService.GetAiQueryStats(from, to);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("aiQueries")]
        public async Task<ActionResult<ServiceResponse<AiQueryPageDto>>> GetAiQueries(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? userId = null,
            [FromQuery] string? vehicleType = null,
            [FromQuery] string? duration = null,
            [FromQuery] string? vibe = null,
            [FromQuery] string? spotType = null,
            [FromQuery] double? radiusKm = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] bool? isSuccess = null,
            [FromQuery] string? model = null)
        {
            var response = await _metricsService.GetAiQueries(
                page, pageSize, userId, vehicleType, duration,
                vibe, spotType, radiusKm, from, to, isSuccess, model);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

    }
}
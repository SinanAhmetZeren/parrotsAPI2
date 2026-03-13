using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services;
using ParrotsAPI2.Dtos;
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
            // if (!CheckAdmin(out var unauthorizedResult)) return unauthorizedResult;
            // APPLIED ADMIN TO THE CONTROLLER
            var response = await _metricsService.GetWeeklyPurchases();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("weeklyTransactions")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyTransactionsDto>>>> GetWeeklyTransactions()
        {

            // if (!CheckAdmin(out var unauthorizedResult)) return unauthorizedResult;
            // APPLIED ADMIN TO THE CONTROLLER
            var response = await _metricsService.GetWeeklyTransactions();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("weeklyVoyages")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVoyagesDto>>>> GetWeeklyVoyages()
        {
            var response = await _metricsService.GetWeeklyVoyagesCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }


        [HttpGet("weeklyVehicles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVehiclesDto>>>> GetWeeklyVehicles()
        {
            var response = await _metricsService.GetWeeklyVehiclesCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("weeklyUsers")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyVehiclesDto>>>> GetWeeklyUsers()
        {
            var response = await _metricsService.GetWeeklyUsersCreated();

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("weeklyBids")]
        [Authorize(Roles = "Admin")]
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

    }
}
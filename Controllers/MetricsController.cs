using Microsoft.AspNetCore.Mvc;
using ParrotsAPI2.Services;
using ParrotsAPI2.Dtos;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // optional, enable if you want admin-only access
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService;
        }


        private bool CheckAdmin(out ActionResult result)
        {
            if (!User.IsInRole("Admin"))
            {
                result = Unauthorized(new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Only admins can access this endpoint."
                });
                return false;
            }
            result = null;
            return true;
        }

        [HttpGet("weeklyPurchases")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyPurchaseDto>>>> GetWeeklyPurchases()
        {
            if (!CheckAdmin(out var unauthorizedResult)) return unauthorizedResult;

            var response = await _metricsService.GetWeeklyPurchases();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("weeklyTransactions")]
        public async Task<ActionResult<ServiceResponse<List<WeeklyTransactionsDto>>>> GetWeeklyTransactions()
        {
            if (!CheckAdmin(out var unauthorizedResult)) return unauthorizedResult;

            var response = await _metricsService.GetWeeklyTransactions();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }


    }
}
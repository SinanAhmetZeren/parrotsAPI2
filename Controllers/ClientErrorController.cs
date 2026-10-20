using Microsoft.AspNetCore.Mvc;

namespace ParrotsAPI2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientErrorController : ControllerBase
    {
        private readonly ILogger<ClientErrorController> _logger;

        public ClientErrorController(ILogger<ClientErrorController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult LogError([FromBody] ClientErrorDto dto)
        {
            _logger.LogError("[CLIENT ERROR] platform={Platform} source={Source} message={Message} stack={Stack}",
                dto.Platform ?? "unknown",
                dto.Source ?? "unknown",
                dto.Message ?? "(no message)",
                dto.Stack ?? "(no stack)");

            return Ok();
        }
    }

    public class ClientErrorDto
    {
        public string? Message { get; set; }
        public string? Stack { get; set; }
        public string? Source { get; set; }
        public string? Platform { get; set; }
    }
}

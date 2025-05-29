using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // You can add more detailed health checks here if you want
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { minVersion = "1.0.25" });
    }
}

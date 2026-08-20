using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParrotsAPI2.Data;

[ApiController]
[Route("api/[controller]")]
public class VersionController(DataContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var config = await context.MobileVersionConfigs
            .FirstOrDefaultAsync(c => c.Key == "MinVersion");
        return Ok(new
        {
            minVersion = config?.Value ?? "1.0.0",
            forceUpdate = config?.ForceUpdate ?? false,
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("minVersion")]
    public async Task<IActionResult> UpdateMinVersion([FromBody] UpdateVersionDto dto)
    {
        var config = await context.MobileVersionConfigs
            .FirstOrDefaultAsync(c => c.Key == "MinVersion");
        if (config == null) return NotFound();
        config.Value = dto.Version;
        config.ForceUpdate = dto.ForceUpdate;
        await context.SaveChangesAsync();
        return Ok(new { minVersion = config.Value, forceUpdate = config.ForceUpdate });
    }
}

public record UpdateVersionDto(string Version, bool ForceUpdate);

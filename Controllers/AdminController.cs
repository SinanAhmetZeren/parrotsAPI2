using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace parrotsAPI2.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private static readonly HashSet<string> _excludedDocs = new(StringComparer.OrdinalIgnoreCase)
    {
        "server-guide.txt"
    };

    private static readonly string[] _docBaseDirs =
    {
        "docs",
    };

    [HttpGet("admin/docs")]
    public IActionResult ListDocs()
    {
        var root = Directory.GetCurrentDirectory();
        var files = new List<string>();
        foreach (var dir in _docBaseDirs)
        {
            var full = Path.Combine(root, dir);
            if (!Directory.Exists(full)) continue;
            foreach (var f in Directory.GetFiles(full, "*.txt", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f);
                if (_excludedDocs.Contains(name)) continue;
                var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
                files.Add(rel);
            }
        }
        return Ok(files.OrderBy(f => f).ToList());
    }

    [HttpGet("admin/docs/{*filePath}")]
    public IActionResult GetDoc(string filePath)
    {
        var root = Directory.GetCurrentDirectory();
        var name = Path.GetFileName(filePath);
        if (_excludedDocs.Contains(name)) return Forbid();
        var full = Path.GetFullPath(Path.Combine(root, filePath));
        if (!full.StartsWith(root)) return BadRequest();
        if (!System.IO.File.Exists(full)) return NotFound();
        var content = System.IO.File.ReadAllText(full);
        return Ok(new { content });
    }

    [HttpPut("admin/docs/{*filePath}")]
    public IActionResult SaveDoc(string filePath, [FromBody] DocSaveRequest body)
    {
        var root = Directory.GetCurrentDirectory();
        var name = Path.GetFileName(filePath);
        if (_excludedDocs.Contains(name)) return Forbid();
        var full = Path.GetFullPath(Path.Combine(root, filePath));
        if (!full.StartsWith(root)) return BadRequest();
        if (!System.IO.File.Exists(full)) return NotFound();
        System.IO.File.WriteAllText(full, body.Content);
        return Ok();
    }
}

public class DocSaveRequest { public string Content { get; set; } = ""; }

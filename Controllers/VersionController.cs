using Microsoft.AspNetCore.Mvc;
using ProgramVersionApi.Services;

namespace ProgramVersionApi.Controllers;

[ApiController]
[Route("api")]
public class VersionController : ControllerBase
{
    private readonly ProgramService _programService;
    
    public VersionController(ProgramService programService)
    {
        _programService = programService;
    }
    
    [HttpGet("version")]
    public async Task<IActionResult> GetVersion([FromQuery] string name)
    {
        // Проверка: имя программы обязательно
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new 
            { 
                error = "Parameter 'name' is required",
                example = "/api/version?name=firefox"
            });
        }
        
        var version = await _programService.GetVersionByNameAsync(name);
        
        if (version == null)
        {
            return NotFound(new 
            { 
                error = $"Program '{name}' not found",
                availablePrograms = new[] { "firefox", "visual studio code", "vlc media player", "python", "blender" }
            });
        }
        
        return Ok(new
        {
            programName = name,
            version = version
        });
    }
}
using Microsoft.AspNetCore.Mvc;
using ProgramVersionApi.Services;

namespace ProgramVersionApi.Controllers;

[ApiController]
[Route("api")]
public class VersionController : ControllerBase
{
    private readonly ProgramService _programService;
    private readonly ILogger<VersionController> _logger;
    
    public VersionController(ProgramService programService, ILogger<VersionController> logger)
    {
        _programService = programService;
        _logger = logger;
    }
    
    [HttpGet("version")]
    public async Task<IActionResult> GetVersion([FromQuery] string name)
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        _logger.LogInformation("Request received for program version. Name: {ProgramName}, IP: {RemoteIP}", name, remoteIp);

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Empty program name requested from IP: {RemoteIP}", remoteIp);
            return BadRequest(new 
            { 
                error = "Parameter 'name' is required",
                example = "/api/version?name=firefox"
            });
        }
        
        var version = await _programService.GetVersionByNameAsync(name);
        
        if (version == null)
        {
            _logger.LogWarning("Program '{ProgramName}' not found. Requested by IP: {RemoteIP}", name, remoteIp);
            return NotFound(new 
            { 
                error = "Program not found"
            });
        }        
        _logger.LogInformation("Successfully found version for '{ProgramName}': {Version}. IP: {RemoteIP}", name, version, remoteIp);
        
        return Ok(new
        {
            programName = name,
            version = version
        });
    }
}
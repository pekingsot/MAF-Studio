using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using System.Security.Claims;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _systemLogService;

    public SystemLogsController(ISystemLogService systemLogService)
    {
        _systemLogService = systemLogService;
    }

    [HttpGet]
    public async Task<ActionResult> GetLogs([FromQuery] string? level, [FromQuery] string? category, [FromQuery] int limit = 100)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var logs = await _systemLogService.GetAsync(level, category, userId, limit);
        return Ok(logs);
    }
}

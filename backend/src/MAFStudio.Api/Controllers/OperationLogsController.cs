using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OperationLogsController : ControllerBase
{
    private readonly IOperationLogService _operationLogService;
    private readonly IUserRepository _userRepository;

    public OperationLogsController(IOperationLogService operationLogService, IUserRepository userRepository)
    {
        _operationLogService = operationLogService;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] long? userId = null, [FromQuery] int limit = 100)
    {
        var logs = await _operationLogService.GetByUserIdAsync(userId, limit);
        
        var result = logs.Select(l => new
        {
            id = l.Id.ToString(),
            userId = l.UserId,
            operation = l.Action,
            module = l.ResourceType,
            description = l.Description,
            details = l.Details,
            ipAddress = l.IpAddress,
            createdAt = l.CreatedAt.ToString("O"),
            statusCode = l.StatusCode,
            requestPath = l.RequestPath,
            errorMessage = l.ErrorMessage,
            user = new { username = GetUsername(l.UserId).Result }
        }).ToList();
        
        return Ok(result);
    }

    private async Task<string> GetUsername(long userId)
    {
        if (userId <= 0)
            return "系统";
        
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Username ?? "未知用户";
    }
}

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IOperationLogService _operationLogService;
    private readonly IUserRepository _userRepository;

    public LogsController(IOperationLogService operationLogService, IUserRepository userRepository)
    {
        _operationLogService = operationLogService;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? module, 
        [FromQuery] string? operation,
        [FromQuery] int limit = 100)
    {
        var logs = await _operationLogService.GetAllAsync(limit);
        
        var filtered = logs.AsEnumerable();
        
        if (!string.IsNullOrEmpty(module))
        {
            filtered = filtered.Where(l => l.ResourceType == module);
        }
        
        if (!string.IsNullOrEmpty(operation))
        {
            filtered = filtered.Where(l => l.Action == operation);
        }
        
        var result = filtered.Select(l => new
        {
            id = l.Id.ToString(),
            userId = l.UserId,
            operation = l.Action,
            module = l.ResourceType,
            description = l.Description,
            details = l.Details,
            ipAddress = l.IpAddress,
            createdAt = l.CreatedAt.ToString("O"),
            user = new { username = GetUsername(l.UserId).Result }
        }).ToList();
        
        return Ok(result);
    }

    private async Task<string> GetUsername(long userId)
    {
        if (userId <= 0)
            return "系统";
        
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.Username ?? "未知用户";
    }
}

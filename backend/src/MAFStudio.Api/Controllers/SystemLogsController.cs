using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using System.Security.Claims;
using MAFStudio.Core.Entities;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _systemLogService;
    private readonly ILogger<SystemLogsController> _logger;

    public SystemLogsController(ISystemLogService systemLogService, ILogger<SystemLogsController> logger)
    {
        _systemLogService = systemLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetLogs(
        [FromQuery] string? level, 
        [FromQuery] string? category, 
        [FromQuery] string? keyword,
        [FromQuery] string? startTime,
        [FromQuery] string? endTime,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var allLogs = await _systemLogService.GetAsync(level, category, null, 1000);
        
        var filteredLogs = allLogs.AsEnumerable();
        
        if (!string.IsNullOrEmpty(keyword))
        {
            filteredLogs = filteredLogs.Where(l => 
                (l.Message?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (l.Category?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        
        if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out var start))
        {
            filteredLogs = filteredLogs.Where(l => l.CreatedAt >= start);
        }
        
        if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out var end))
        {
            filteredLogs = filteredLogs.Where(l => l.CreatedAt <= end);
        }
        
        var total = filteredLogs.Count();
        var data = filteredLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                id = l.Id.ToString(),
                level = l.Level,
                category = l.Category,
                message = l.Message,
                exception = l.Exception,
                stackTrace = l.StackTrace,
                requestPath = l.RequestPath,
                requestMethod = (string?)null,
                userId = l.UserId,
                userName = (string?)null,
                ipAddress = (string?)null,
                createdAt = l.CreatedAt.ToString("O")
            })
            .ToList();

        return Ok(new
        {
            data,
            page,
            pageSize,
            total
        });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] int days = 7)
    {
        var logs = await _systemLogService.GetAsync(null, null, null, 10000);
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var recentLogs = logs.Where(l => l.CreatedAt >= startDate).ToList();
        
        var levelCounts = recentLogs
            .GroupBy(l => l.Level)
            .Select(g => new { level = g.Key, count = g.Count() })
            .ToList();
        
        var dailyCounts = recentLogs
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .OrderBy(d => d.date)
            .ToList();
        
        var totalErrors = recentLogs.Count(l => l.Level == "Error" || l.Level == "Critical");

        return Ok(new
        {
            levelCounts,
            dailyCounts,
            totalErrors,
            periodDays = days
        });
    }

    [HttpGet("levels")]
    public ActionResult GetLevels()
    {
        return Ok(new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" });
    }

    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories()
    {
        var logs = await _systemLogService.GetAsync(null, null, null, 10000);
        var categories = logs
            .Where(l => !string.IsNullOrEmpty(l.Category))
            .Select(l => l.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        return Ok(categories);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        var result = await _systemLogService.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return Ok(new { success = true });
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> Clear([FromQuery] int? beforeDays)
    {
        var logs = await _systemLogService.GetAsync(null, null, null, 100000);
        var cutoffDate = beforeDays.HasValue 
            ? DateTime.UtcNow.AddDays(-beforeDays.Value) 
            : DateTime.MaxValue;
        
        var toDelete = logs.Where(l => l.CreatedAt < cutoffDate).ToList();
        var deletedCount = 0;
        
        foreach (var log in toDelete)
        {
            if (await _systemLogService.DeleteAsync(log.Id))
            {
                deletedCount++;
            }
        }
        
        return Ok(new { deletedCount });
    }
}

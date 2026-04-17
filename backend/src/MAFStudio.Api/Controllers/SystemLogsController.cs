using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;

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
        DateTime? startDt = null;
        DateTime? endDt = null;

        if (!string.IsNullOrEmpty(startTime) && DateTime.TryParse(startTime, out var s))
            startDt = s;
        if (!string.IsNullOrEmpty(endTime) && DateTime.TryParse(endTime, out var e))
            endDt = e;

        var (data, total) = await _systemLogService.GetPagedAsync(level, category, keyword, startDt, endDt, page, pageSize);

        return Ok(new
        {
            data = data.Select(l => new
            {
                id = l.Id.ToString(),
                level = l.Level,
                category = l.Category,
                message = l.Message,
                exception = l.Exception,
                stackTrace = l.StackTrace,
                requestPath = l.RequestPath,
                requestMethod = l.RequestMethod,
                source = l.Source,
                userId = l.UserId,
                createdAt = l.CreatedAt.ToString("O")
            }),
            page,
            pageSize,
            total
        });
    }

    [HttpGet("statistics")]
    public async Task<ActionResult> GetStatistics([FromQuery] int days = 7)
    {
        var levelCounts = await _systemLogService.GetLevelCountsAsync(days);
        var dailyCounts = await _systemLogService.GetDailyCountsAsync(days);

        return Ok(new
        {
            levelCounts = levelCounts.Select(kv => new { level = kv.Key, count = kv.Value }),
            dailyCounts,
            totalErrors = levelCounts.Where(kv => kv.Key is "Error" or "Critical").Sum(kv => kv.Value),
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
        var categories = await _systemLogService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        var result = await _systemLogService.DeleteAsync(id);
        if (!result)
            return NotFound();
        return Ok(new { success = true });
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> Clear([FromQuery] int beforeDays = 30)
    {
        var deletedCount = await _systemLogService.ClearBeforeAsync(beforeDays);
        return Ok(new { deletedCount });
    }
}

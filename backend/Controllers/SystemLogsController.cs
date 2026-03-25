using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public SystemLogsController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetLogs(
            [FromQuery] string? level,
            [FromQuery] string? category,
            [FromQuery] string? keyword,
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var query = _context.SystemLogs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level == level);
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(l => l.Category != null && l.Category.Contains(category));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(l => 
                    l.Message.Contains(keyword) || 
                    (l.Exception != null && l.Exception.Contains(keyword)));
            }

            if (startTime.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endTime.Value);
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Level,
                    l.Category,
                    l.Message,
                    l.Exception,
                    l.StackTrace,
                    l.RequestPath,
                    l.RequestMethod,
                    l.UserId,
                    l.UserName,
                    l.IpAddress,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                data = logs
            });
        }

        [HttpGet("levels")]
        public async Task<ActionResult<List<string>>> GetLogLevels()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var levels = await _context.SystemLogs
                .Select(l => l.Level)
                .Distinct()
                .ToListAsync();

            return Ok(levels);
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var categories = await _context.SystemLogs
                .Where(l => l.Category != null)
                .Select(l => l.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SystemLog>> GetLog(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var log = await _context.SystemLogs.FindAsync(id);

            if (log == null)
            {
                return NotFound();
            }

            return Ok(log);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteLog(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var log = await _context.SystemLogs.FindAsync(id);

            if (log == null)
            {
                return NotFound();
            }

            _context.SystemLogs.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("clear")]
        public async Task<ActionResult> ClearLogs([FromQuery] int? beforeDays)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var cutoffDate = beforeDays.HasValue
                ? DateTime.UtcNow.AddDays(-beforeDays.Value)
                : DateTime.UtcNow;

            var logsToDelete = await _context.SystemLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.SystemLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { deletedCount = logsToDelete.Count });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics([FromQuery] int days = 7)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);

            if (!isAdmin)
            {
                return Forbid();
            }

            var startDate = DateTime.UtcNow.AddDays(-days);

            var levelCounts = await _context.SystemLogs
                .Where(l => l.CreatedAt >= startDate)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToListAsync();

            var dailyLogs = await _context.SystemLogs
                .Where(l => l.CreatedAt >= startDate)
                .Select(l => l.CreatedAt)
                .ToListAsync();

            var dailyCounts = dailyLogs
                .GroupBy(d => d.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToList();

            var totalErrors = await _context.SystemLogs
                .Where(l => l.CreatedAt >= startDate && (l.Level == "Error" || l.Level == "Critical"))
                .CountAsync();

            return Ok(new
            {
                levelCounts,
                dailyCounts,
                totalErrors,
                periodDays = days
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LogsController : ControllerBase
    {
        private readonly IOperationLogService _logService;
        private readonly IAuthService _authService;

        public LogsController(IOperationLogService logService, IAuthService authService)
        {
            _logService = logService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<List<OperationLog>>> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? module = null,
            [FromQuery] string? operation = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var logs = await _logService.GetLogsAsync(userId!, isAdmin, page, pageSize, module, operation);
            return Ok(logs);
        }
    }

    public class OperationLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public User? User { get; set; }
    }

    public class User
    {
        public string Username { get; set; } = string.Empty;
    }
}
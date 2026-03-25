using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Models;
using MAFStudio.Backend.Models.Responses;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgentRuntimeController : ControllerBase
    {
        private readonly IAgentRuntimeService _runtimeService;
        private readonly IAgentService _agentService;
        private readonly IAuthService _authService;

        public AgentRuntimeController(
            IAgentRuntimeService runtimeService,
            IAgentService agentService,
            IAuthService authService)
        {
            _runtimeService = runtimeService;
            _agentService = agentService;
            _authService = authService;
        }

        [HttpGet("{id}/status")]
        public async Task<ActionResult<AgentRuntimeStatusResponse>> GetStatus(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }

            var instance = await _runtimeService.GetRuntimeInstanceAsync(id);
            
            return Ok(new AgentRuntimeStatusResponse
            {
                AgentId = id,
                State = instance?.State.ToString() ?? "Uninitialized",
                LastActiveTime = instance?.LastActiveTime,
                TaskCount = instance?.TaskCount ?? 0,
                LastError = instance?.LastError,
                IsAlive = instance != null && instance.State != AgentRuntimeState.Uninitialized && instance.State != AgentRuntimeState.Error
            });
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<AgentRuntimeStatusResponse>> Activate(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                var instance = await _runtimeService.ActivateAgentAsync(id);
                
                await _agentService.UpdateAgentStatusAsync(id, AgentStatus.Active);
                
                return Ok(new AgentRuntimeStatusResponse
                {
                    AgentId = id,
                    State = instance.State.ToString(),
                    LastActiveTime = instance.LastActiveTime,
                    TaskCount = instance.TaskCount,
                    LastError = instance.LastError,
                    IsAlive = instance.State == AgentRuntimeState.Ready || instance.State == AgentRuntimeState.Busy
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"激活失败: {ex.Message}" });
            }
        }

        [HttpPost("{id}/sleep")]
        public async Task<ActionResult<AgentRuntimeStatusResponse>> Sleep(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                await _runtimeService.SleepAgentAsync(id);
                
                await _agentService.UpdateAgentStatusAsync(id, AgentStatus.Inactive);
                
                var instance = await _runtimeService.GetRuntimeInstanceAsync(id);
                
                return Ok(new AgentRuntimeStatusResponse
                {
                    AgentId = id,
                    State = instance?.State.ToString() ?? "Sleeping",
                    LastActiveTime = instance?.LastActiveTime,
                    TaskCount = instance?.TaskCount ?? 0,
                    LastError = instance?.LastError,
                    IsAlive = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"休眠失败: {ex.Message}" });
            }
        }

        [HttpPost("{id}/destroy")]
        public async Task<ActionResult<AgentRuntimeStatusResponse>> Destroy(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                await _runtimeService.DestroyAgentAsync(id);
                
                await _agentService.UpdateAgentStatusAsync(id, AgentStatus.Inactive);
                
                return Ok(new AgentRuntimeStatusResponse
                {
                    AgentId = id,
                    State = "Uninitialized",
                    LastActiveTime = null,
                    TaskCount = 0,
                    LastError = null,
                    IsAlive = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"销毁失败: {ex.Message}" });
            }
        }

        [HttpPost("{id}/test")]
        public async Task<ActionResult<AgentTestResponse>> Test(Guid id, [FromBody] AgentTestRequest? request = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                var (success, message, latencyMs) = await _runtimeService.TestAgentAsync(id);
                
                return Ok(new AgentTestResponse
                {
                    Success = success,
                    Message = message,
                    LatencyMs = latencyMs
                });
            }
            catch (Exception ex)
            {
                return Ok(new AgentTestResponse
                {
                    Success = false,
                    Message = $"测试失败: {ex.Message}",
                    LatencyMs = 0
                });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<AgentRuntimeStatusResponse>>> GetActiveAgents()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var activeAgents = _runtimeService.GetActiveAgents();
            
            var result = new List<AgentRuntimeStatusResponse>();
            
            foreach (var kvp in activeAgents)
            {
                var agent = await _agentService.GetAgentByIdAsync(kvp.Key);
                if (agent == null) continue;
                
                if (!isAdmin && agent.UserId != userId)
                {
                    continue;
                }
                
                result.Add(new AgentRuntimeStatusResponse
                {
                    AgentId = kvp.Key,
                    State = kvp.Value.State.ToString(),
                    LastActiveTime = kvp.Value.LastActiveTime,
                    TaskCount = kvp.Value.TaskCount,
                    LastError = kvp.Value.LastError,
                    IsAlive = kvp.Value.State == AgentRuntimeState.Ready || kvp.Value.State == AgentRuntimeState.Busy
                });
            }
            
            return Ok(result);
        }
    }
}

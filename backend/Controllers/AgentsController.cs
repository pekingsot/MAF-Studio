using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models.Requests;
using MAFStudio.Backend.Models.VOs;
using MAFStudio.Backend.Models.Mappers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgentsController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IAuthService _authService;
        private readonly IOperationLogService _logService;
        private readonly ApplicationDbContext _dbContext;

        public AgentsController(IAgentService agentService, IAuthService authService, IOperationLogService logService, ApplicationDbContext dbContext)
        {
            _agentService = agentService;
            _authService = authService;
            _logService = logService;
            _dbContext = dbContext;
        }

        [HttpGet("{id}/debug-config")]
        public async Task<ActionResult> GetAgentDebugConfig(Guid id)
        {
            var agent = await _dbContext.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (agent == null)
            {
                return NotFound(new { error = "Agent not found" });
            }

            var llmConfig = agent.LLMConfigId.HasValue
                ? await _dbContext.LLMConfigs
                    .Include(c => c.Models)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == agent.LLMConfigId.Value)
                : null;

            return Ok(new
            {
                Agent = new
                {
                    agent.Id,
                    agent.Name,
                    agent.LLMConfigId,
                    agent.LLMModelConfigId
                },
                LLMConfig = llmConfig == null ? null : new
                {
                    llmConfig.Id,
                    llmConfig.Name,
                    llmConfig.Provider,
                    llmConfig.Endpoint,
                    Models = llmConfig.Models?.Select(m => new
                    {
                        m.Id,
                        m.ModelName,
                        m.DisplayName,
                        m.IsDefault
                    }).ToList()
                }
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<AgentListItemVo>>> GetAllAgents()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agents = await _agentService.GetAgentsByUserIdAsync(userId!, isAdmin);
            var vos = agents.Select(a => a.ToListItemVo()).ToList();
            return Ok(vos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AgentVo>> GetAgent(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }
            
            return Ok(agent.ToVo());
        }

        [HttpPost]
        public async Task<ActionResult<Agent>> CreateAgent([FromBody] CreateAgentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var agent = await _agentService.CreateAgentAsync(
                request.Name,
                request.Description,
                request.Type,
                request.Configuration,
                request.Avatar,
                userId!,
                request.LLMConfigId
            );
            
            await _logService.LogAsync(userId!, "创建", "智能体", $"创建智能体: {request.Name}", 
                System.Text.Json.JsonSerializer.Serialize(request));
            
            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, agent);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Agent>> UpdateAgent(Guid id, [FromBody] UpdateAgentRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var existingAgent = await _agentService.GetAgentByIdAsync(id);
            if (existingAgent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && existingAgent.UserId != userId)
            {
                return Forbid();
            }
            
            var agent = await _agentService.UpdateAgentAsync(id, request.Name, request.Description, request.Configuration, request.Avatar, request.LLMConfigId);
            
            await _logService.LogAsync(userId!, "修改", "智能体", $"修改智能体: {request.Name}",
                System.Text.Json.JsonSerializer.Serialize(request));
            
            return Ok(agent);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAgent(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var existingAgent = await _agentService.GetAgentByIdAsync(id);
            if (existingAgent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && existingAgent.UserId != userId)
            {
                return Forbid();
            }
            
            await _logService.LogAsync(userId!, "删除", "智能体", $"删除智能体: {existingAgent.Name}",
                System.Text.Json.JsonSerializer.Serialize(new { id, name = existingAgent.Name }));
            
            var result = await _agentService.DeleteAgentAsync(id);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<Agent>> UpdateAgentStatus(Guid id, [FromBody] UpdateAgentStatusRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var existingAgent = await _agentService.GetAgentByIdAsync(id);
            if (existingAgent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && existingAgent.UserId != userId)
            {
                return Forbid();
            }
            
            var agent = await _agentService.UpdateAgentStatusAsync(id, request.Status);
            return Ok(agent);
        }
    }
}
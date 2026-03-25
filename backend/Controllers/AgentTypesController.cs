using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    /// <summary>
    /// 智能体类型控制器
    /// 提供智能体类型的管理接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AgentTypesController : ControllerBase
    {
        private readonly IAgentTypeService _agentTypeService;
        private readonly IAuthService _authService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AgentTypesController(IAgentTypeService agentTypeService, IAuthService authService)
        {
            _agentTypeService = agentTypeService;
            _authService = authService;
        }

        /// <summary>
        /// 获取所有智能体类型（系统类型+用户自己的类型）
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AgentType>>> GetAllTypes()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var types = await _agentTypeService.GetAllTypesAsync(userId);
            return Ok(types);
        }

        /// <summary>
        /// 获取启用的智能体类型
        /// </summary>
        [HttpGet("enabled")]
        public async Task<ActionResult<List<AgentType>>> GetEnabledTypes()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var types = await _agentTypeService.GetEnabledTypesAsync(userId);
            return Ok(types);
        }

        /// <summary>
        /// 根据ID获取类型
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AgentType>> GetType(Guid id)
        {
            var type = await _agentTypeService.GetByIdAsync(id);
            if (type == null)
            {
                return NotFound();
            }
            return Ok(type);
        }

        /// <summary>
        /// 创建智能体类型
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<AgentType>> CreateType([FromBody] AgentType type)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var created = await _agentTypeService.CreateTypeAsync(type, userId);
            return CreatedAtAction(nameof(GetType), new { id = created.Id }, created);
        }

        /// <summary>
        /// 更新智能体类型
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AgentType>> UpdateType(Guid id, [FromBody] AgentType type)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            var updated = await _agentTypeService.UpdateTypeAsync(id, type, isAdmin ? null : userId);
            if (updated == null)
            {
                return NotFound(new { message = "类型不存在或无权限修改" });
            }
            return Ok(updated);
        }

        /// <summary>
        /// 删除智能体类型
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteType(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _agentTypeService.DeleteTypeAsync(id, userId);
            if (!result)
            {
                return BadRequest(new { message = "无法删除系统内置类型或无权限删除" });
            }
            return NoContent();
        }

        /// <summary>
        /// 启用/禁用智能体类型
        /// </summary>
        [HttpPatch("{id}/enable")]
        public async Task<ActionResult<AgentType>> ToggleEnable(Guid id, [FromBody] ToggleEnableRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            var type = await _agentTypeService.ToggleEnableAsync(id, request.IsEnabled, isAdmin ? null : userId);
            if (type == null)
            {
                return NotFound(new { message = "类型不存在或无权限修改" });
            }
            return Ok(type);
        }

        /// <summary>
        /// 初始化默认类型（仅管理员）
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeDefaultTypes()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            if (!isAdmin)
            {
                return Forbid();
            }
            await _agentTypeService.InitializeDefaultTypesAsync();
            return Ok();
        }
    }

    /// <summary>
    /// 启用/禁用请求
    /// </summary>
    public class ToggleEnableRequest
    {
        public bool IsEnabled { get; set; }
    }
}

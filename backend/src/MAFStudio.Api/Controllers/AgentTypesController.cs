using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Entities;
using System.Security.Claims;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentTypesController : ControllerBase
{
    private readonly IAgentTypeRepository _agentTypeRepository;
    private readonly ILogger<AgentTypesController> _logger;

    public AgentTypesController(IAgentTypeRepository agentTypeRepository, ILogger<AgentTypesController> logger)
    {
        _agentTypeRepository = agentTypeRepository;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<AgentType>>> GetAll()
    {
        try
        {
            var types = await _agentTypeRepository.GetAllAsync();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取智能体类型列表失败");
            return StatusCode(500, new { message = "获取智能体类型列表失败" });
        }
    }

    [HttpGet("enabled")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AgentType>>> GetEnabled()
    {
        try
        {
            var types = await _agentTypeRepository.GetEnabledAsync();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用的智能体类型列表失败");
            return StatusCode(500, new { message = "获取启用的智能体类型列表失败" });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<AgentType>> GetById(long id)
    {
        try
        {
            var type = await _agentTypeRepository.GetByIdAsync(id);
            if (type == null)
            {
                return NotFound(new { message = "智能体类型不存在" });
            }
            return Ok(type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取智能体类型失败: {Id}", id);
            return StatusCode(500, new { message = "获取智能体类型失败" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AgentType>> Create([FromBody] CreateAgentTypeRequest request)
    {
        try
        {
            _logger.LogInformation("创建智能体类型请求: {@Request}", request);
            
            var existingType = await _agentTypeRepository.GetByCodeAsync(request.Code);
            if (existingType != null)
            {
                _logger.LogWarning("类型编码已存在: {Code}", request.Code);
                return BadRequest(new { message = "类型编码已存在" });
            }

            var agentType = new AgentType
            {
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Icon = request.Icon,
                IsSystem = false,
                IsEnabled = request.IsEnabled,
                SortOrder = request.SortOrder
            };
            agentType.SetDefaultConfiguration(request.DefaultSystemPrompt, request.DefaultTemperature, request.DefaultMaxTokens);

            _logger.LogInformation("准备插入数据库: {@AgentType}", new { agentType.Name, agentType.Code, agentType.DefaultConfiguration });
            
            var created = await _agentTypeRepository.CreateAsync(agentType);
            
            _logger.LogInformation("插入成功，返回结果: {@Created}", created);
            
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建智能体类型失败");
            return StatusCode(500, new { message = "创建智能体类型失败: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AgentType>> Update(long id, [FromBody] UpdateAgentTypeRequest request)
    {
        try
        {
            var existingType = await _agentTypeRepository.GetByIdAsync(id);
            if (existingType == null)
            {
                return NotFound(new { message = "智能体类型不存在" });
            }

            existingType.Name = request.Name;
            existingType.Description = request.Description;
            existingType.Icon = request.Icon;
            existingType.IsEnabled = request.IsEnabled;
            existingType.SortOrder = request.SortOrder;
            existingType.SetDefaultConfiguration(request.DefaultSystemPrompt, request.DefaultTemperature, request.DefaultMaxTokens);

            var updated = await _agentTypeRepository.UpdateAsync(existingType);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新智能体类型失败: {Id}", id);
            return StatusCode(500, new { message = "更新智能体类型失败" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var existingType = await _agentTypeRepository.GetByIdAsync(id);
            if (existingType == null)
            {
                return NotFound(new { message = "智能体类型不存在" });
            }

            if (existingType.IsSystem)
            {
                return BadRequest(new { message = "系统内置类型不能删除" });
            }

            var result = await _agentTypeRepository.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "智能体类型不存在" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除智能体类型失败: {Id}", id);
            return StatusCode(500, new { message = "删除智能体类型失败" });
        }
    }

    [HttpPatch("{id}/enable")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult> ToggleEnable(long id, [FromBody] ToggleEnableRequest request)
    {
        try
        {
            var existingType = await _agentTypeRepository.GetByIdAsync(id);
            if (existingType == null)
            {
                return NotFound(new { message = "智能体类型不存在" });
            }

            existingType.IsEnabled = request.IsEnabled;
            await _agentTypeRepository.UpdateAsync(existingType);
            return Ok(new { message = request.IsEnabled ? "已启用" : "已禁用" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换智能体类型状态失败: {Id}", id);
            return StatusCode(500, new { message = "操作失败" });
        }
    }
}

public class CreateAgentTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? DefaultSystemPrompt { get; set; }
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 4096;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class UpdateAgentTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? DefaultSystemPrompt { get; set; }
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 4096;
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

public class ToggleEnableRequest
{
    public bool IsEnabled { get; set; }
}

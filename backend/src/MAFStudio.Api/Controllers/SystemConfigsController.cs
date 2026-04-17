using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SystemConfigsController : ControllerBase
{
    private readonly ISystemConfigRepository _repository;
    private readonly ILogger<SystemConfigsController> _logger;

    public SystemConfigsController(ISystemConfigRepository repository, ILogger<SystemConfigsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        try
        {
            var configs = await _repository.GetAllAsync();
            return Ok(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统配置失败");
            return BadRequest(new { message = $"获取系统配置失败: {ex.Message}" });
        }
    }

    [HttpGet("{key}")]
    public async Task<ActionResult> GetByKey(string key)
    {
        try
        {
            var config = await _repository.GetByKeyAsync(key);
            if (config == null) return NotFound(new { message = "配置不存在" });
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置失败: Key={Key}", key);
            return BadRequest(new { message = $"获取配置失败: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(long id, [FromBody] SystemConfigUpdateDto dto)
    {
        try
        {
            var config = await _repository.GetByIdAsync(id);
            if (config == null) return NotFound(new { message = "配置不存在" });

            config.Value = dto.Value;
            config.Description = dto.Description ?? config.Description;
            var updated = await _repository.UpdateAsync(config);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新配置失败: Id={Id}", id);
            return BadRequest(new { message = $"更新配置失败: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] SystemConfigCreateDto dto)
    {
        try
        {
            var existing = await _repository.GetByKeyAsync(dto.Key);
            if (existing != null) return BadRequest(new { message = "配置键已存在" });

            var config = new SystemConfig
            {
                Key = dto.Key,
                Value = dto.Value ?? "",
                Description = dto.Description ?? "",
            };
            var created = await _repository.CreateAsync(config);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建配置失败");
            return BadRequest(new { message = $"创建配置失败: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "配置不存在" });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除配置失败: Id={Id}", id);
            return BadRequest(new { message = $"删除配置失败: {ex.Message}" });
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult> BatchSave([FromBody] SystemConfigBatchDto dto)
    {
        try
        {
            foreach (var item in dto.Configs)
            {
                var existing = await _repository.GetByKeyAsync(item.Key);
                if (existing != null)
                {
                    existing.Value = item.Value;
                    existing.Description = item.Description ?? existing.Description;
                    await _repository.UpdateAsync(existing);
                }
                else
                {
                    var config = new SystemConfig
                    {
                        Key = item.Key,
                        Value = item.Value ?? "",
                        Description = item.Description ?? "",
                    };
                    await _repository.CreateAsync(config);
                }
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量保存配置失败");
            return BadRequest(new { message = $"批量保存配置失败: {ex.Message}" });
        }
    }
}

public class SystemConfigUpdateDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SystemConfigCreateDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

public class SystemConfigBatchDto
{
    public List<SystemConfigBatchItem> Configs { get; set; } = new();
}

public class SystemConfigBatchItem
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

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

    public SystemConfigsController(ISystemConfigRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var configs = await _repository.GetAllAsync();
        return Ok(configs);
    }

    [HttpGet("{key}")]
    public async Task<ActionResult> GetByKey(string key)
    {
        var config = await _repository.GetByKeyAsync(key);
        if (config == null) return NotFound(new { message = "配置不存在" });
        return Ok(config);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(long id, [FromBody] SystemConfigUpdateDto dto)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null) return NotFound(new { message = "配置不存在" });

        config.Value = dto.Value;
        config.Description = dto.Description ?? config.Description;
        var updated = await _repository.UpdateAsync(config);
        return Ok(updated);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] SystemConfigCreateDto dto)
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

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = "配置不存在" });
        return Ok(new { success = true });
    }

    [HttpPost("batch")]
    public async Task<ActionResult> BatchSave([FromBody] SystemConfigBatchDto dto)
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

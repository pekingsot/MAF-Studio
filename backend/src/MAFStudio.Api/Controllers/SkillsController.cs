using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MAFStudio.Application.Skills;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly IAgentSkillRepository _skillRepository;
    private readonly ISkillTemplateRepository _templateRepository;
    private readonly SkillLoader _skillLoader;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SkillsController(
        IAgentSkillRepository skillRepository,
        ISkillTemplateRepository templateRepository,
        SkillLoader skillLoader)
    {
        _skillRepository = skillRepository;
        _templateRepository = templateRepository;
        _skillLoader = skillLoader;
    }

    [HttpGet("agent/{agentId}")]
    public async Task<ActionResult> GetAgentSkills(long agentId)
    {
        var skills = await _skillRepository.GetByAgentIdAsync(agentId);
        return Ok(new { success = true, data = skills.Select(MapToDto) });
    }

    [HttpGet("agent/{agentId}/enabled")]
    public async Task<ActionResult> GetEnabledAgentSkills(long agentId)
    {
        var skills = await _skillRepository.GetEnabledByAgentIdAsync(agentId);
        var definitions = new List<object>();

        foreach (var skill in skills)
        {
            var def = _skillLoader.ParseSkillContent(skill.SkillContent);
            definitions.Add(new
            {
                skill.Id,
                skill.SkillName,
                skill.Enabled,
                skill.Priority,
                skill.Runtime,
                def.Description,
                def.AllowedTools,
                def.Tags,
                def.Permissions
            });
        }

        return Ok(new { success = true, data = definitions });
    }

    [HttpPost("agent/{agentId}")]
    public async Task<ActionResult> AddSkillToAgent(long agentId, [FromBody] AddSkillRequest request)
    {
        var existing = await _skillRepository.GetByAgentIdAsync(agentId);
        if (existing.Any(s => s.SkillName == request.SkillName))
        {
            return BadRequest(new { success = false, message = $"技能 {request.SkillName} 已存在" });
        }

        var skill = new AgentSkill
        {
            AgentId = agentId,
            SkillName = request.SkillName,
            SkillContent = request.SkillContent,
            Enabled = request.Enabled ?? true,
            Priority = request.Priority ?? 0,
            Runtime = request.Runtime ?? "python",
            EntryPoint = request.EntryPoint,
            AllowedTools = request.AllowedTools != null
                ? JsonSerializer.Serialize(request.AllowedTools) : null,
            Permissions = request.Permissions != null
                ? JsonSerializer.Serialize(request.Permissions) : null,
            Parameters = request.Parameters != null
                ? JsonSerializer.Serialize(request.Parameters) : null
        };

        var created = await _skillRepository.CreateAsync(skill);
        _skillLoader.ClearCache();

        return Ok(new { success = true, data = MapToDto(created) });
    }

    [HttpPut("agent/{agentId}/{skillId}")]
    public async Task<ActionResult> UpdateSkill(long agentId, long skillId, [FromBody] UpdateSkillRequest request)
    {
        var skill = await _skillRepository.GetByIdAsync(skillId);
        if (skill == null || skill.AgentId != agentId)
        {
            return NotFound(new { success = false, message = "技能不存在" });
        }

        if (request.SkillContent != null) skill.SkillContent = request.SkillContent;
        if (request.Enabled.HasValue) skill.Enabled = request.Enabled.Value;
        if (request.Priority.HasValue) skill.Priority = request.Priority.Value;
        if (request.Runtime != null) skill.Runtime = request.Runtime;
        if (request.EntryPoint != null) skill.EntryPoint = request.EntryPoint;
        if (request.AllowedTools != null)
            skill.AllowedTools = JsonSerializer.Serialize(request.AllowedTools);
        if (request.Permissions != null)
            skill.Permissions = JsonSerializer.Serialize(request.Permissions);

        var updated = await _skillRepository.UpdateAsync(skill);
        _skillLoader.ClearCache();

        return Ok(new { success = true, data = MapToDto(updated) });
    }

    [HttpDelete("agent/{agentId}/{skillId}")]
    public async Task<ActionResult> DeleteSkill(long agentId, long skillId)
    {
        var skill = await _skillRepository.GetByIdAsync(skillId);
        if (skill == null || skill.AgentId != agentId)
        {
            return NotFound(new { success = false, message = "技能不存在" });
        }

        await _skillRepository.DeleteAsync(skillId);
        _skillLoader.ClearCache();

        return Ok(new { success = true });
    }

    [HttpPost("agent/{agentId}/from-template/{templateId}")]
    public async Task<ActionResult> AddSkillFromTemplate(long agentId, long templateId, [FromBody] AddFromTemplateRequest? request = null)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return NotFound(new { success = false, message = "模板不存在" });
        }

        var skillName = request?.SkillName ?? template.Name;

        var existing = await _skillRepository.GetByAgentIdAsync(agentId);
        if (existing.Any(s => s.SkillName == skillName))
        {
            return BadRequest(new { success = false, message = $"技能 {skillName} 已存在" });
        }

        var skill = new AgentSkill
        {
            AgentId = agentId,
            SkillName = skillName,
            SkillContent = template.Content,
            Enabled = true,
            Priority = request?.Priority ?? 0,
            Runtime = template.Runtime
        };

        var created = await _skillRepository.CreateAsync(skill);
        await _templateRepository.IncrementUsageCountAsync(templateId);
        _skillLoader.ClearCache();

        return Ok(new { success = true, data = MapToDto(created) });
    }

    [HttpGet("templates")]
    public async Task<ActionResult> GetTemplates([FromQuery] string? category = null)
    {
        var templates = string.IsNullOrEmpty(category)
            ? await _templateRepository.GetAllAsync()
            : await _templateRepository.GetByCategoryAsync(category);

        return Ok(new
        {
            success = true,
            data = templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Category,
                t.Tags,
                t.Runtime,
                t.UsageCount,
                t.IsOfficial
            })
        });
    }

    [HttpGet("templates/{id}")]
    public async Task<ActionResult> GetTemplate(long id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
        {
            return NotFound(new { success = false, message = "模板不存在" });
        }

        return Ok(new { success = true, data = template });
    }

    [HttpPost("templates")]
    public async Task<ActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var template = new SkillTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Category = request.Category,
            Tags = request.Tags,
            Runtime = request.Runtime ?? "python",
            IsOfficial = false
        };

        var created = await _templateRepository.CreateAsync(template);
        return Ok(new { success = true, data = created });
    }

    [HttpPost("parse")]
    public ActionResult ParseSkillContent([FromBody] ParseSkillRequest request)
    {
        try
        {
            var definition = _skillLoader.ParseSkillContent(request.Content);
            return Ok(new
            {
                success = true,
                data = new
                {
                    definition.Name,
                    definition.Description,
                    definition.Version,
                    definition.Author,
                    definition.AllowedTools,
                    definition.Tags,
                    definition.Permissions,
                    definition.Inputs,
                    definition.Outputs,
                    definition.Runtime,
                    definition.Instructions,
                    InstructionsLength = definition.Instructions.Length
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"解析失败: {ex.Message}" });
        }
    }

    private static object MapToDto(AgentSkill skill)
    {
        List<string>? allowedTools = null;
        if (skill.AllowedTools != null)
        {
            try { allowedTools = JsonSerializer.Deserialize<List<string>>(skill.AllowedTools); } catch { }
        }

        return new
        {
            skill.Id,
            skill.AgentId,
            skill.SkillName,
            SkillContent = skill.SkillContent,
            skill.Enabled,
            skill.Priority,
            skill.Runtime,
            skill.EntryPoint,
            AllowedTools = allowedTools,
            skill.CreatedAt,
            skill.UpdatedAt
        };
    }
}

public class AddSkillRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string SkillContent { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
    public int? Priority { get; set; }
    public string? Runtime { get; set; }
    public string? EntryPoint { get; set; }
    public List<string>? AllowedTools { get; set; }
    public Dictionary<string, bool>? Permissions { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
}

public class UpdateSkillRequest
{
    public string? SkillContent { get; set; }
    public bool? Enabled { get; set; }
    public int? Priority { get; set; }
    public string? Runtime { get; set; }
    public string? EntryPoint { get; set; }
    public List<string>? AllowedTools { get; set; }
    public Dictionary<string, bool>? Permissions { get; set; }
}

public class AddFromTemplateRequest
{
    public string? SkillName { get; set; }
    public int? Priority { get; set; }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? Runtime { get; set; }
}

public class ParseSkillRequest
{
    public string Content { get; set; } = string.Empty;
}

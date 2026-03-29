using MAFStudio.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SkillLoader _skillLoader;
    private readonly SkillExecutor _skillExecutor;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(
        SkillLoader skillLoader,
        SkillExecutor skillExecutor,
        ILogger<SkillsController> logger)
    {
        _skillLoader = skillLoader;
        _skillExecutor = skillExecutor;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<Skill>> GetAllSkills()
    {
        var skills = _skillLoader.GetAllLoadedSkills();
        return Ok(skills);
    }

    [HttpPost("load")]
    public async Task<ActionResult<Skill>> LoadSkill([FromBody] LoadSkillRequest request)
    {
        try
        {
            var skill = await _skillLoader.LoadSkillAsync(request.Path);
            if (skill == null)
            {
                return NotFound(new { error = "Skill加载失败" });
            }

            return Ok(skill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载Skill失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("load-all")]
    public async Task<ActionResult<List<Skill>>> LoadAllSkills()
    {
        try
        {
            var skills = await _skillLoader.LoadAllSkillsAsync();
            return Ok(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载所有Skill失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{skillId}")]
    public ActionResult<Skill> GetSkill(string skillId)
    {
        var skill = _skillLoader.GetSkill(skillId);
        if (skill == null)
        {
            return NotFound(new { error = $"Skill {skillId} 不存在" });
        }

        return Ok(skill);
    }

    [HttpDelete("{skillId}")]
    public ActionResult UnloadSkill(string skillId)
    {
        var result = _skillLoader.UnloadSkill(skillId);
        if (!result)
        {
            return NotFound(new { error = $"Skill {skillId} 不存在" });
        }

        return Ok(new { message = "Skill已卸载" });
    }

    [HttpPost("{skillId}/execute")]
    public async Task<ActionResult> ExecuteSkill(
        string skillId,
        [FromBody] ExecuteSkillRequest request)
    {
        try
        {
            var result = await _skillExecutor.ExecuteSkillAsync(
                skillId,
                request.Parameters);

            return Ok(new { result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Skill失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class LoadSkillRequest
{
    public string Path { get; set; } = string.Empty;
}

public class ExecuteSkillRequest
{
    public Dictionary<string, object>? Parameters { get; set; }
}

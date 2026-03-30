using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAFStudio.Api.Controllers;

/// <summary>
/// 工作流模板控制器
/// </summary>
[ApiController]
[Route("api/workflow-templates")]
[Authorize]
public class WorkflowTemplatesController : ControllerBase
{
    private readonly IWorkflowTemplateService _templateService;
    private readonly ILogger<WorkflowTemplatesController> _logger;

    public WorkflowTemplatesController(
        IWorkflowTemplateService templateService,
        ILogger<WorkflowTemplatesController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有工作流模板
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<WorkflowTemplateDto>>> GetAll(
        [FromQuery] bool? isPublic = null,
        [FromQuery] string? category = null)
    {
        try
        {
            var templates = await _templateService.GetAllTemplatesAsync(isPublic, category);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作流模板列表失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取工作流模板
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowTemplateDto>> GetById(long id)
    {
        try
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound(new { success = false, error = $"模板 {id} 不存在" });
            }
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取工作流模板 {id} 失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索工作流模板
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<WorkflowTemplateDto>>> Search(
        [FromQuery] string keyword,
        [FromQuery] string? tags = null)
    {
        try
        {
            var tagList = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            var templates = await _templateService.SearchTemplatesAsync(keyword, tagList);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索工作流模板失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 创建工作流模板
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<WorkflowTemplateDto>> Create([FromBody] CreateWorkflowTemplateRequest request)
    {
        try
        {
            var template = await _templateService.CreateTemplateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工作流模板失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 更新工作流模板
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowTemplateDto>> Update(long id, [FromBody] UpdateWorkflowTemplateRequest request)
    {
        try
        {
            var template = await _templateService.UpdateTemplateAsync(id, request);
            if (template == null)
            {
                return NotFound(new { success = false, error = $"模板 {id} 不存在" });
            }
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"更新工作流模板 {id} 失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 删除工作流模板
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var success = await _templateService.DeleteTemplateAsync(id);
            if (!success)
            {
                return NotFound(new { success = false, error = $"模板 {id} 不存在" });
            }
            return Ok(new { success = true, message = "删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除工作流模板 {id} 失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 执行工作流模板
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<ActionResult<CollaborationResult>> Execute(long id, [FromBody] ExecuteTemplateRequest request)
    {
        try
        {
            var result = await _templateService.ExecuteTemplateAsync(id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"执行工作流模板 {id} 失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 生成Magentic计划
    /// </summary>
    [HttpPost("magentic/generate")]
    public async Task<ActionResult<GenerateMagenticPlanResponse>> GenerateMagenticPlan(
        [FromBody] GenerateMagenticPlanRequest request)
    {
        try
        {
            var result = await _templateService.GenerateMagenticPlanAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成Magentic计划失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 保存Magentic计划为模板
    /// </summary>
    [HttpPost("magentic/save")]
    public async Task<ActionResult<WorkflowTemplateDto>> SaveMagenticPlan(
        [FromBody] SaveMagenticPlanRequest request)
    {
        try
        {
            var template = await _templateService.SaveMagenticPlanAsync(request);
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存Magentic计划失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 查找匹配的模板
    /// </summary>
    [HttpPost("match")]
    public async Task<ActionResult<WorkflowTemplateDto?>> FindMatchingTemplate([FromBody] FindMatchingTemplateRequest request)
    {
        try
        {
            var template = await _templateService.FindMatchingTemplateAsync(request.Task);
            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找匹配模板失败");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

/// <summary>
/// 查找匹配模板请求
/// </summary>
public class FindMatchingTemplateRequest
{
    /// <summary>
    /// 任务描述
    /// </summary>
    public string Task { get; set; } = string.Empty;
}

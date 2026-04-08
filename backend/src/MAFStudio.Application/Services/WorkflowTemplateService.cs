using System.Text.Json;
using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

/// <summary>
/// 工作流模板服务实现
/// </summary>
public class WorkflowTemplateService : IWorkflowTemplateService
{
    private readonly IWorkflowTemplateRepository _templateRepository;
    private readonly ICollaborationWorkflowService _workflowService;
    private readonly ILogger<WorkflowTemplateService> _logger;

    public WorkflowTemplateService(
        IWorkflowTemplateRepository templateRepository,
        ICollaborationWorkflowService workflowService,
        ILogger<WorkflowTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _workflowService = workflowService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<WorkflowTemplateDto>> GetAllTemplatesAsync(bool? isPublic = null, string? category = null)
    {
        var templates = await _templateRepository.GetAllAsync(isPublic, category);
        return templates.Select(MapToDto).ToList();
    }

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    public async Task<WorkflowTemplateDto?> GetTemplateByIdAsync(long id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        return template != null ? MapToDto(template) : null;
    }

    /// <summary>
    /// 搜索模板
    /// </summary>
    public async Task<List<WorkflowTemplateDto>> SearchTemplatesAsync(string keyword, List<string>? tags = null)
    {
        var templates = await _templateRepository.SearchAsync(keyword, tags);
        return templates.Select(MapToDto).ToList();
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest request, long? userId = null)
    {
        var template = new WorkflowTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags != null ? JsonSerializer.Serialize(request.Tags) : null,
            WorkflowDefinition = JsonSerializer.Serialize(request.Workflow),
            Parameters = request.Parameters != null ? JsonSerializer.Serialize(request.Parameters) : null,
            CreatedBy = userId,
            IsPublic = request.IsPublic,
            Source = request.Source,
            OriginalTask = request.OriginalTask
        };

        var created = await _templateRepository.CreateAsync(template);
        return MapToDto(created);
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    public async Task<WorkflowTemplateDto?> UpdateTemplateAsync(long id, UpdateWorkflowTemplateRequest request)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null) return null;

        if (request.Name != null) template.Name = request.Name;
        if (request.Description != null) template.Description = request.Description;
        if (request.Category != null) template.Category = request.Category;
        if (request.Tags != null) template.Tags = JsonSerializer.Serialize(request.Tags);
        if (request.Workflow != null) template.WorkflowDefinition = JsonSerializer.Serialize(request.Workflow);
        if (request.Parameters != null) template.Parameters = JsonSerializer.Serialize(request.Parameters);
        if (request.IsPublic.HasValue) template.IsPublic = request.IsPublic.Value;

        var updated = await _templateRepository.UpdateAsync(template);
        return MapToDto(updated);
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(long id)
    {
        return await _templateRepository.DeleteAsync(id);
    }

    /// <summary>
    /// 执行模板工作流
    /// </summary>
    public async Task<CollaborationResult> ExecuteTemplateAsync(long templateId, ExecuteTemplateRequest request)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
        {
            return new CollaborationResult
            {
                Success = false,
                Error = $"模板 {templateId} 不存在"
            };
        }

        await _templateRepository.IncrementUsageCountAsync(templateId);

        var workflowDefinition = JsonSerializer.Deserialize<WorkflowDefinitionDto>(template.WorkflowDefinition);
        if (workflowDefinition == null)
        {
            return new CollaborationResult
            {
                Success = false,
                Error = "工作流定义格式错误"
            };
        }

        if (request.ParameterValues != null)
        {
            workflowDefinition = ApplyParameters(workflowDefinition, request.ParameterValues);
        }

        return await _workflowService.ExecuteCustomWorkflowAsync(
            request.CollaborationId,
            workflowDefinition,
            request.Input);
    }

    /// <summary>
    /// 生成Magentic计划
    /// </summary>
    public async Task<GenerateMagenticPlanResponse> GenerateMagenticPlanAsync(GenerateMagenticPlanRequest request)
    {
        try
        {
            var matchingTemplate = await FindMatchingTemplateAsync(request.Task);
            
            if (matchingTemplate != null)
            {
                _logger.LogInformation($"找到匹配的模板: {matchingTemplate.Name}");
                
                return new GenerateMagenticPlanResponse
                {
                    Success = true,
                    Workflow = matchingTemplate.Workflow
                };
            }

            var workflow = await _workflowService.GenerateMagenticPlanAsync(
                request.CollaborationId,
                request.Task);

            return new GenerateMagenticPlanResponse
            {
                Success = true,
                Workflow = workflow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"生成Magentic计划失败: {ex.Message}");
            return new GenerateMagenticPlanResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 保存Magentic计划为模板
    /// </summary>
    public async Task<WorkflowTemplateDto> SaveMagenticPlanAsync(SaveMagenticPlanRequest request)
    {
        var createRequest = new CreateWorkflowTemplateRequest
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            Workflow = request.Workflow,
            Parameters = request.Parameters,
            IsPublic = request.IsPublic,
            Source = request.EnableLearning ? "magentic_optimized" : "magentic",
            OriginalTask = request.OriginalTask
        };

        return await CreateTemplateAsync(createRequest);
    }

    /// <summary>
    /// 查找匹配的模板
    /// </summary>
    public async Task<WorkflowTemplateDto?> FindMatchingTemplateAsync(string task)
    {
        var taskTags = ExtractTaskFeatures(task);
        
        var matchedTemplates = await _templateRepository.FindByTagsAsync(taskTags, minSimilarity: 0.8);
        
        return matchedTemplates.FirstOrDefault() != null ? MapToDto(matchedTemplates.First()) : null;
    }

    /// <summary>
    /// 提取任务特征
    /// </summary>
    private List<string> ExtractTaskFeatures(string task)
    {
        var keywords = new List<string>();
        
        var aiKeywords = new[] { "AI", "模型", "能效", "研究", "报告", "分析", "翻译", "代码", "审查", "优化" };
        
        foreach (var keyword in aiKeywords)
        {
            if (task.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add(keyword);
            }
        }

        return keywords;
    }

    /// <summary>
    /// 应用参数到工作流定义
    /// </summary>
    private WorkflowDefinitionDto ApplyParameters(
        WorkflowDefinitionDto workflow,
        Dictionary<string, object> parameterValues)
    {
        var workflowJson = JsonSerializer.Serialize(workflow);

        foreach (var param in parameterValues)
        {
            workflowJson = workflowJson.Replace($"{{{{{param.Key}}}}}", param.Value?.ToString() ?? "");
        }

        return JsonSerializer.Deserialize<WorkflowDefinitionDto>(workflowJson) ?? workflow;
    }

    /// <summary>
    /// 映射到DTO
    /// </summary>
    private WorkflowTemplateDto MapToDto(WorkflowTemplate template)
    {
        var workflow = new WorkflowDefinitionDto();
        var parameters = new Dictionary<string, ParameterDefinitionDto>();
        var tags = new List<string>();

        try
        {
            if (!string.IsNullOrEmpty(template.WorkflowDefinition))
            {
                workflow = JsonSerializer.Deserialize<WorkflowDefinitionDto>(template.WorkflowDefinition) ?? workflow;
            }

            if (!string.IsNullOrEmpty(template.Parameters))
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, ParameterDefinitionDto>>(template.Parameters) 
                    ?? parameters;
            }

            if (!string.IsNullOrEmpty(template.Tags))
            {
                tags = JsonSerializer.Deserialize<List<string>>(template.Tags) ?? tags;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"解析模板 {template.Id} 的JSON失败");
        }

        return new WorkflowTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Tags = tags,
            Workflow = workflow,
            Parameters = parameters,
            CreatedBy = template.CreatedBy,
            IsPublic = template.IsPublic,
            UsageCount = template.UsageCount,
            Source = template.Source,
            OriginalTask = template.OriginalTask,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt ?? DateTime.UtcNow
        };
    }
}

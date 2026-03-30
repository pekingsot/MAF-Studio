using MAFStudio.Application.DTOs;

namespace MAFStudio.Application.Interfaces;

/// <summary>
/// 工作流模板服务接口
/// </summary>
public interface IWorkflowTemplateService
{
    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<List<WorkflowTemplateDto>> GetAllTemplatesAsync(bool? isPublic = null, string? category = null);

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    Task<WorkflowTemplateDto?> GetTemplateByIdAsync(long id);

    /// <summary>
    /// 搜索模板
    /// </summary>
    Task<List<WorkflowTemplateDto>> SearchTemplatesAsync(string keyword, List<string>? tags = null);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<WorkflowTemplateDto> CreateTemplateAsync(CreateWorkflowTemplateRequest request, long? userId = null);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task<WorkflowTemplateDto?> UpdateTemplateAsync(long id, UpdateWorkflowTemplateRequest request);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task<bool> DeleteTemplateAsync(long id);

    /// <summary>
    /// 执行模板工作流
    /// </summary>
    Task<CollaborationResult> ExecuteTemplateAsync(long templateId, ExecuteTemplateRequest request);

    /// <summary>
    /// 生成Magentic计划
    /// </summary>
    Task<GenerateMagenticPlanResponse> GenerateMagenticPlanAsync(GenerateMagenticPlanRequest request);

    /// <summary>
    /// 保存Magentic计划为模板
    /// </summary>
    Task<WorkflowTemplateDto> SaveMagenticPlanAsync(SaveMagenticPlanRequest request);

    /// <summary>
    /// 查找匹配的模板
    /// </summary>
    Task<WorkflowTemplateDto?> FindMatchingTemplateAsync(string task);
}

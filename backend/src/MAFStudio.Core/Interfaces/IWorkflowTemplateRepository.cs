using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces;

/// <summary>
/// 工作流模板仓储接口
/// </summary>
public interface IWorkflowTemplateRepository
{
    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    Task<WorkflowTemplate?> GetByIdAsync(long id);

    /// <summary>
    /// 获取所有模板
    /// </summary>
    Task<List<WorkflowTemplate>> GetAllAsync(bool? isPublic = null, string? category = null);

    /// <summary>
    /// 搜索模板
    /// </summary>
    Task<List<WorkflowTemplate>> SearchAsync(string keyword, List<string>? tags = null);

    /// <summary>
    /// 创建模板
    /// </summary>
    Task<WorkflowTemplate> CreateAsync(WorkflowTemplate template);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task<WorkflowTemplate> UpdateAsync(WorkflowTemplate template);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 增加使用次数
    /// </summary>
    Task IncrementUsageCountAsync(long id);

    /// <summary>
    /// 根据标签查找匹配的模板
    /// </summary>
    Task<List<WorkflowTemplate>> FindByTagsAsync(List<string> tags, double minSimilarity = 0.8);
}

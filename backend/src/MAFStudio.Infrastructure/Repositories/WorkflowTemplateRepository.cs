using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces;
using MAFStudio.Infrastructure.Data;
using System.Text.Json;

namespace MAFStudio.Infrastructure.Repositories;

/// <summary>
/// 工作流模板仓储实现
/// </summary>
public class WorkflowTemplateRepository : IWorkflowTemplateRepository
{
    private readonly IDapperContext _context;

    public WorkflowTemplateRepository(IDapperContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    public async Task<WorkflowTemplate?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateOpenConnection();
        var sql = @"
            SELECT id, name, description, category, tags, workflow_definition, parameters,
                   created_by, is_public, usage_count, source, original_task, created_at, updated_at
            FROM workflow_templates
            WHERE id = @Id";

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });

        if (result == null) return null;

        return MapToEntity(result);
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<WorkflowTemplate>> GetAllAsync(bool? isPublic = null, string? category = null)
    {
        using var connection = _context.CreateOpenConnection();
        
        var sql = @"
            SELECT id, name, description, category, tags, workflow_definition, parameters,
                   created_by, is_public, usage_count, source, original_task, created_at, updated_at
            FROM workflow_templates
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (isPublic.HasValue)
        {
            sql += " AND is_public = @IsPublic";
            parameters.Add("IsPublic", isPublic.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND category = @Category";
            parameters.Add("Category", category);
        }

        sql += " ORDER BY usage_count DESC, created_at DESC";

        var results = await connection.QueryAsync<dynamic>(sql, parameters);

        return results.Select(MapToEntity).ToList();
    }

    /// <summary>
    /// 搜索模板
    /// </summary>
    public async Task<List<WorkflowTemplate>> SearchAsync(string keyword, List<string>? tags = null)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = @"
            SELECT id, name, description, category, tags, workflow_definition, parameters,
                   created_by, is_public, usage_count, source, original_task, created_at, updated_at
            FROM workflow_templates
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(keyword))
        {
            sql += @" AND (name ILIKE @Keyword 
                         OR description ILIKE @Keyword 
                         OR original_task ILIKE @Keyword)";
            parameters.Add("Keyword", $"%{keyword}%");
        }

        if (tags != null && tags.Any())
        {
            sql += " AND tags @> @Tags::jsonb";
            parameters.Add("Tags", JsonSerializer.Serialize(tags));
        }

        sql += " ORDER BY usage_count DESC, created_at DESC";

        var results = await connection.QueryAsync<dynamic>(sql, parameters);

        return results.Select(MapToEntity).ToList();
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<WorkflowTemplate> CreateAsync(WorkflowTemplate template)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = @"
            INSERT INTO workflow_templates 
                (name, description, category, tags, workflow_definition, parameters, 
                 created_by, is_public, usage_count, source, original_task, created_at, updated_at)
            VALUES 
                (@Name, @Description, @Category, @Tags::jsonb, @WorkflowDefinition, @Parameters::jsonb,
                 @CreatedBy, @IsPublic, @UsageCount, @Source, @OriginalTask, NOW(), NOW())
            RETURNING id";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            template.Name,
            template.Description,
            template.Category,
            Tags = template.Tags,
            template.WorkflowDefinition,
            Parameters = template.Parameters ?? "{}",
            template.CreatedBy,
            template.IsPublic,
            template.UsageCount,
            template.Source,
            template.OriginalTask
        });

        template.Id = id;
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        return template;
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    public async Task<WorkflowTemplate> UpdateAsync(WorkflowTemplate template)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = @"
            UPDATE workflow_templates 
            SET name = @Name, 
                description = @Description, 
                category = @Category, 
                tags = @Tags::jsonb,
                workflow_definition = @WorkflowDefinition,
                parameters = @Parameters::jsonb,
                is_public = @IsPublic,
                updated_at = NOW()
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            Tags = template.Tags,
            template.WorkflowDefinition,
            Parameters = template.Parameters ?? "{}",
            template.IsPublic
        });

        template.UpdatedAt = DateTime.UtcNow;

        return template;
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = "DELETE FROM workflow_templates WHERE id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

        return rowsAffected > 0;
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    public async Task IncrementUsageCountAsync(long id)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = @"
            UPDATE workflow_templates 
            SET usage_count = usage_count + 1, updated_at = NOW()
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>
    /// 根据标签查找匹配的模板
    /// </summary>
    public async Task<List<WorkflowTemplate>> FindByTagsAsync(List<string> tags, double minSimilarity = 0.8)
    {
        using var connection = _context.CreateOpenConnection();

        var sql = @"
            SELECT id, name, description, category, tags, workflow_definition, parameters,
                   created_by, is_public, usage_count, source, original_task, created_at, updated_at
            FROM workflow_templates
            WHERE source IN ('magentic', 'magentic_optimized')
            ORDER BY usage_count DESC, created_at DESC";

        var results = await connection.QueryAsync<dynamic>(sql);

        var templates = results.Select(MapToEntity).ToList();

        var matchedTemplates = new List<WorkflowTemplate>();

        foreach (var template in templates)
        {
            var similarity = CalculateSimilarity(tags, template.Tags, template.OriginalTask);
            if (similarity >= minSimilarity)
            {
                matchedTemplates.Add(template);
            }
        }

        return matchedTemplates.OrderByDescending(t => t.UsageCount).ToList();
    }

    /// <summary>
    /// 计算相似度
    /// </summary>
    private double CalculateSimilarity(List<string> taskTags, string? templateTagsJson, string? originalTask)
    {
        if (!taskTags.Any()) return 0;

        var templateTags = new List<string>();
        if (!string.IsNullOrEmpty(templateTagsJson))
        {
            try
            {
                templateTags = JsonSerializer.Deserialize<List<string>>(templateTagsJson) ?? new List<string>();
            }
            catch
            {
                templateTags = new List<string>();
            }
        }

        var matchCount = 0;

        foreach (var tag in taskTags)
        {
            if (templateTags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase)))
            {
                matchCount++;
            }
            else if (!string.IsNullOrEmpty(originalTask) && 
                     originalTask.Contains(tag, StringComparison.OrdinalIgnoreCase))
            {
                matchCount++;
            }
        }

        return (double)matchCount / taskTags.Count;
    }

    /// <summary>
    /// 映射到实体
    /// </summary>
    private WorkflowTemplate MapToEntity(dynamic result)
    {
        return new WorkflowTemplate
        {
            Id = result.id,
            Name = result.name,
            Description = result.description,
            Category = result.category,
            Tags = result.tags,
            WorkflowDefinition = result.workflow_definition,
            Parameters = result.parameters,
            CreatedBy = result.created_by,
            IsPublic = result.is_public,
            UsageCount = result.usage_count,
            Source = result.source,
            OriginalTask = result.original_task,
            CreatedAt = result.created_at,
            UpdatedAt = result.updated_at
        };
    }
}

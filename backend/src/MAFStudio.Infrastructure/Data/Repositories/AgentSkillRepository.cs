using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Utils;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentSkillRepository : IAgentSkillRepository
{
    private readonly IDapperContext _context;

    public AgentSkillRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<AgentSkill?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_skills WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<AgentSkill>(sql, new { Id = id });
    }

    public async Task<List<AgentSkill>> GetByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_skills WHERE agent_id = @AgentId ORDER BY priority DESC, created_at ASC";
        var result = await connection.QueryAsync<AgentSkill>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<List<AgentSkill>> GetEnabledByAgentIdAsync(long agentId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_skills WHERE agent_id = @AgentId AND enabled = true ORDER BY priority DESC, created_at ASC";
        var result = await connection.QueryAsync<AgentSkill>(sql, new { AgentId = agentId });
        return result.ToList();
    }

    public async Task<AgentSkill> CreateAsync(AgentSkill skill)
    {
        using var connection = _context.CreateConnection();
        skill.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO agent_skills (agent_id, skill_name, skill_content, enabled, priority, runtime, 
                entry_point, allowed_tools, permissions, parameters, created_at, updated_at)
            VALUES (@AgentId, @SkillName, @SkillContent, @Enabled, @Priority, @Runtime, 
                @EntryPoint, @AllowedTools, @Permissions, @Parameters, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<AgentSkill>(sql, skill);
    }

    public async Task<AgentSkill> UpdateAsync(AgentSkill skill)
    {
        using var connection = _context.CreateConnection();
        skill.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE agent_skills SET 
                skill_name = @SkillName,
                skill_content = @SkillContent,
                enabled = @Enabled,
                priority = @Priority,
                runtime = @Runtime,
                entry_point = @EntryPoint,
                allowed_tools = @AllowedTools,
                permissions = @Permissions,
                parameters = @Parameters,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<AgentSkill>(sql, skill);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_skills WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> DeleteByAgentAndNameAsync(long agentId, string skillName)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_skills WHERE agent_id = @AgentId AND skill_name = @SkillName";
        var rows = await connection.ExecuteAsync(sql, new { AgentId = agentId, SkillName = skillName });
        return rows > 0;
    }
}

public class SkillTemplateRepository : ISkillTemplateRepository
{
    private readonly IDapperContext _context;

    public SkillTemplateRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<SkillTemplate?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM skill_templates WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<SkillTemplate>(sql, new { Id = id });
    }

    public async Task<List<SkillTemplate>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM skill_templates ORDER BY is_official DESC, usage_count DESC, name ASC";
        var result = await connection.QueryAsync<SkillTemplate>(sql);
        return result.ToList();
    }

    public async Task<List<SkillTemplate>> GetByCategoryAsync(string category)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM skill_templates WHERE category = @Category ORDER BY is_official DESC, usage_count DESC";
        var result = await connection.QueryAsync<SkillTemplate>(sql, new { Category = category });
        return result.ToList();
    }

    public async Task<SkillTemplate> CreateAsync(SkillTemplate template)
    {
        using var connection = _context.CreateConnection();
        template.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO skill_templates (name, description, content, category, tags, runtime, usage_count, is_official, created_at, updated_at)
            VALUES (@Name, @Description, @Content, @Category, @Tags, @Runtime, @UsageCount, @IsOfficial, @CreatedAt, @UpdatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<SkillTemplate>(sql, template);
    }

    public async Task<SkillTemplate> UpdateAsync(SkillTemplate template)
    {
        using var connection = _context.CreateConnection();
        template.UpdatedAt = DateTime.UtcNow;
        const string sql = @"
            UPDATE skill_templates SET 
                name = @Name,
                description = @Description,
                content = @Content,
                category = @Category,
                tags = @Tags,
                runtime = @Runtime,
                is_official = @IsOfficial,
                updated_at = @UpdatedAt
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<SkillTemplate>(sql, template);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM skill_templates WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task IncrementUsageCountAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "UPDATE skill_templates SET usage_count = usage_count + 1 WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}

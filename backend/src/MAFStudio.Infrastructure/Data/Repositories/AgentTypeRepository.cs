using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class AgentTypeRepository : IAgentTypeRepository
{
    private readonly IDapperContext _context;

    public AgentTypeRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<AgentType?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_types WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<AgentType>(sql, new { Id = id });
    }

    public async Task<AgentType?> GetByCodeAsync(string code)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_types WHERE code = @Code";
        return await connection.QueryFirstOrDefaultAsync<AgentType>(sql, new { Code = code });
    }

    public async Task<List<AgentType>> GetAllAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_types ORDER BY sort_order, name";
        var result = await connection.QueryAsync<AgentType>(sql);
        return result.ToList();
    }

    public async Task<List<AgentType>> GetEnabledAsync()
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM agent_types WHERE is_enabled = true ORDER BY sort_order, name";
        var result = await connection.QueryAsync<AgentType>(sql);
        return result.ToList();
    }

    public async Task<AgentType> CreateAsync(AgentType agentType)
    {
        using var connection = _context.CreateConnection();
        agentType.CreatedAt = DateTime.UtcNow;
        var defaultConfig = string.IsNullOrEmpty(agentType.DefaultConfiguration) 
            ? "{}" 
            : agentType.DefaultConfiguration;
        const string sql = @"
            INSERT INTO agent_types (name, code, description, icon, default_configuration, llm_config_id, is_system, is_enabled, sort_order, created_at)
            VALUES (@Name, @Code, @Description, @Icon, @DefaultConfig::jsonb, @LlmConfigId, @IsSystem, @IsEnabled, @SortOrder, @CreatedAt)
            RETURNING *";
        return await connection.QueryFirstAsync<AgentType>(sql, new 
        {
            agentType.Name,
            agentType.Code,
            agentType.Description,
            agentType.Icon,
            DefaultConfig = defaultConfig,
            agentType.LlmConfigId,
            agentType.IsSystem,
            agentType.IsEnabled,
            agentType.SortOrder,
            agentType.CreatedAt
        });
    }

    public async Task<AgentType> UpdateAsync(AgentType agentType)
    {
        using var connection = _context.CreateConnection();
        var defaultConfig = string.IsNullOrEmpty(agentType.DefaultConfiguration) 
            ? "{}" 
            : agentType.DefaultConfiguration;
        const string sql = @"
            UPDATE agent_types SET 
                name = @Name,
                code = @Code,
                description = @Description,
                icon = @Icon,
                default_configuration = @DefaultConfig::jsonb,
                llm_config_id = @LlmConfigId,
                is_system = @IsSystem,
                is_enabled = @IsEnabled,
                sort_order = @SortOrder
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<AgentType>(sql, new 
        {
            agentType.Id,
            agentType.Name,
            agentType.Code,
            agentType.Description,
            agentType.Icon,
            DefaultConfig = defaultConfig,
            agentType.LlmConfigId,
            agentType.IsSystem,
            agentType.IsEnabled,
            agentType.SortOrder
        });
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM agent_types WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}

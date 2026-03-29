using Dapper;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Infrastructure.Data.Repositories;

public class LlmModelConfigRepository : ILlmModelConfigRepository
{
    private readonly IDapperContext _context;

    public LlmModelConfigRepository(IDapperContext context)
    {
        _context = context;
    }

    public async Task<LlmModelConfig?> GetByIdAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_model_configs WHERE id = @Id";
        return await connection.QueryFirstOrDefaultAsync<LlmModelConfig>(sql, new { Id = id });
    }

    public async Task<List<LlmModelConfig>> GetByLlmConfigIdAsync(long llmConfigId)
    {
        using var connection = _context.CreateConnection();
        const string sql = "SELECT * FROM llm_model_configs WHERE llm_config_id = @LlmConfigId ORDER BY sort_order, id";
        var result = await connection.QueryAsync<LlmModelConfig>(sql, new { LlmConfigId = llmConfigId });
        return result.ToList();
    }

    public async Task<LlmModelConfig> CreateAsync(LlmModelConfig config)
    {
        using var connection = _context.CreateConnection();
        config.CreatedAt = DateTime.UtcNow;
        const string sql = @"
            INSERT INTO llm_model_configs (
                llm_config_id, model_name, display_name, description, 
                is_default, is_enabled, sort_order, temperature, max_tokens, 
                context_window, top_p, frequency_penalty, presence_penalty, 
                stop_sequences, availability_status, created_at
            ) VALUES (
                @LlmConfigId, @ModelName, @DisplayName, @Description,
                @IsDefault, @IsEnabled, @SortOrder, @Temperature, @MaxTokens,
                @ContextWindow, @TopP, @FrequencyPenalty, @PresencePenalty,
                @StopSequences, @AvailabilityStatus, @CreatedAt
            )
            RETURNING *";
        return await connection.QueryFirstAsync<LlmModelConfig>(sql, config);
    }

    public async Task<LlmModelConfig> UpdateAsync(LlmModelConfig config)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE llm_model_configs SET 
                model_name = @ModelName,
                display_name = @DisplayName,
                description = @Description,
                is_default = @IsDefault,
                is_enabled = @IsEnabled,
                sort_order = @SortOrder,
                temperature = @Temperature,
                max_tokens = @MaxTokens,
                context_window = @ContextWindow,
                top_p = @TopP,
                frequency_penalty = @FrequencyPenalty,
                presence_penalty = @PresencePenalty,
                stop_sequences = @StopSequences,
                last_test_time = @LastTestTime,
                availability_status = @AvailabilityStatus,
                test_result = @TestResult
            WHERE id = @Id
            RETURNING *";
        return await connection.QueryFirstAsync<LlmModelConfig>(sql, config);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _context.CreateConnection();
        const string sql = "DELETE FROM llm_model_configs WHERE id = @Id";
        var rows = await connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task SetDefaultAsync(long llmConfigId, long modelId)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "UPDATE llm_model_configs SET is_default = false WHERE llm_config_id = @LlmConfigId",
                new { LlmConfigId = llmConfigId },
                transaction);
            
            await connection.ExecuteAsync(
                "UPDATE llm_model_configs SET is_default = true WHERE id = @Id",
                new { Id = modelId },
                transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateTestStatusAsync(long modelId, bool isAvailable, string testResult)
    {
        using var connection = _context.CreateConnection();
        const string sql = @"
            UPDATE llm_model_configs 
            SET last_test_time = @LastTestTime,
                availability_status = @AvailabilityStatus,
                test_result = @TestResult
            WHERE id = @Id";
        
        await connection.ExecuteAsync(sql, new
        {
            Id = modelId,
            LastTestTime = DateTime.UtcNow,
            AvailabilityStatus = isAvailable ? 1 : 0,
            TestResult = testResult
        });
    }
}

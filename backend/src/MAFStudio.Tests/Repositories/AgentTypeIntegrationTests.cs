using MAFStudio.Core.Entities;
using MAFStudio.Infrastructure.Data.Repositories;
using Npgsql;
using Dapper;
using Xunit;

namespace MAFStudio.Tests.Repositories;

public class AgentTypeIntegrationTests
{
    private const string ConnectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";

    public AgentTypeIntegrationTests()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    [Fact]
    public async Task CreateAsync_ShouldInsertAgentType()
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var agentType = new AgentType
        {
            Name = "测试类型_" + Guid.NewGuid().ToString("N")[..8],
            Code = "test_" + Guid.NewGuid().ToString("N")[..8],
            Description = "测试描述",
            Icon = "🤖",
            IsSystem = false,
            IsEnabled = true,
            SortOrder = 0
        };
        agentType.SetDefaultConfiguration("你是一个测试助手", 0.8, 8192);

        Console.WriteLine($"[DEBUG] DefaultConfiguration: {agentType.DefaultConfiguration}");

        const string sql = @"
            INSERT INTO agent_types (name, code, description, icon, default_configuration, llm_config_id, is_system, is_enabled, sort_order, created_at)
            VALUES (@Name, @Code, @Description, @Icon, @DefaultConfiguration::jsonb, @LlmConfigId, @IsSystem, @IsEnabled, @SortOrder, @CreatedAt)
            RETURNING *";

        try
        {
            var result = await connection.QueryFirstAsync<AgentType>(sql, new
            {
                agentType.Name,
                agentType.Code,
                agentType.Description,
                agentType.Icon,
                DefaultConfiguration = agentType.DefaultConfiguration ?? "{}",
                agentType.LlmConfigId,
                agentType.IsSystem,
                agentType.IsEnabled,
                agentType.SortOrder,
                agentType.CreatedAt
            });

            Console.WriteLine($"[DEBUG] Inserted Id: {result.Id}");
            Console.WriteLine($"[DEBUG] Inserted DefaultConfiguration: {result.DefaultConfiguration}");
            Console.WriteLine($"[DEBUG] Inserted DefaultTemperature: {result.DefaultTemperature}");
            Console.WriteLine($"[DEBUG] Inserted DefaultMaxTokens: {result.DefaultMaxTokens}");

            Assert.True(result.Id > 0);
            Assert.Equal(agentType.Name, result.Name);
            Assert.Equal(0.8, result.DefaultTemperature, 2);
            Assert.Equal(8192, result.DefaultMaxTokens);

            await connection.ExecuteAsync("DELETE FROM agent_types WHERE id = @Id", new { result.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            throw;
        }
    }
}

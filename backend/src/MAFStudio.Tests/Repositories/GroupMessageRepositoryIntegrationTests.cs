using Dapper;
using MAFStudio.Api.Controllers;
using MAFStudio.Core.Entities;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MAFStudio.Tests.Repositories;

public class GroupMessageRepositoryIntegrationTests
{
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123;Timezone=Asia/Shanghai";

    private IDapperContext CreateDapperContext()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            })
            .Build();

        return new DapperContext(configuration);
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveModelName_ToDatabase()
    {
        var context = CreateDapperContext();
        var repository = new GroupMessageRepository(context);

        var message = new GroupMessage
        {
            CollaborationId = 1000,
            MessageType = "chat",
            SenderType = "Agent",
            FromAgentId = 1004,
            FromAgentName = "测试协调者",
            FromAgentRole = "Manager",
            FromAgentType = "协调者",
            FromAgentAvatar = "🦞",
            ModelName = "qwen3.5-plus-test",
            Content = "集成测试：验证model_name入库",
            IsMentioned = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(message);

        Assert.True(created.Id > 0, "消息应该成功插入数据库并获得ID");

        var retrieved = await repository.GetByCollaborationIdAsync(1000, 1, null);
        var savedMessage = retrieved.FirstOrDefault(m => m.Id == created.Id);

        Assert.NotNull(savedMessage);
        Assert.Equal("qwen3.5-plus-test", savedMessage.ModelName);
        Assert.Equal("测试协调者", savedMessage.FromAgentName);
        Assert.Equal("Agent", savedMessage.SenderType);

        using var connection = context.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM group_messages WHERE id = @Id", new { Id = created.Id });
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveNullModelName_ToDatabase()
    {
        var context = CreateDapperContext();
        var repository = new GroupMessageRepository(context);

        var message = new GroupMessage
        {
            CollaborationId = 1000,
            MessageType = "chat",
            SenderType = "User",
            ModelName = null,
            Content = "集成测试：验证model_name为null",
            IsMentioned = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(message);

        Assert.True(created.Id > 0, "消息应该成功插入数据库并获得ID");

        var retrieved = await repository.GetByCollaborationIdAsync(1000, 1, null);
        var savedMessage = retrieved.FirstOrDefault(m => m.Id == created.Id);

        Assert.NotNull(savedMessage);
        Assert.Null(savedMessage.ModelName);

        using var connection = context.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM group_messages WHERE id = @Id", new { Id = created.Id });
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveModelNameFromLlmConfigs_ToDatabase()
    {
        var context = CreateDapperContext();
        var repository = new GroupMessageRepository(context);

        var llmConfigs = "[{\"llmConfigId\":1001,\"llmConfigName\":\"千问-2841\",\"llmModelConfigId\":1350,\"modelName\":\"qwen3.5-plus\",\"isPrimary\":true,\"priority\":0,\"isValid\":true,\"msg\":\"\"}]";

        string? modelName = null;
        if (!string.IsNullOrEmpty(llmConfigs))
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var configs = System.Text.Json.JsonSerializer.Deserialize<List<LlmConfigItem>>(llmConfigs, jsonOptions);
            modelName = configs?.FirstOrDefault(c => c.IsPrimary)?.ModelName;
        }

        Assert.Equal("qwen3.5-plus", modelName);

        var message = new GroupMessage
        {
            CollaborationId = 1000,
            MessageType = "chat",
            SenderType = "Agent",
            FromAgentId = 1000,
            FromAgentName = "产品经理",
            ModelName = modelName,
            Content = "集成测试：验证从LlmConfigs解析model_name入库",
            IsMentioned = false,
            CreatedAt = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(message);

        Assert.True(created.Id > 0);

        var retrieved = await repository.GetByCollaborationIdAsync(1000, 1, null);
        var savedMessage = retrieved.FirstOrDefault(m => m.Id == created.Id);

        Assert.NotNull(savedMessage);
        Assert.Equal("qwen3.5-plus", savedMessage.ModelName);

        using var connection = context.CreateOpenConnection();
        await connection.ExecuteAsync("DELETE FROM group_messages WHERE id = @Id", new { Id = created.Id });
    }
}

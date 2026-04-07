using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MAFStudio.Tests.Repositories;

public class CollaborationTaskRepositoryIntegrationTests
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
    public async Task CreateAsync_WithPrompt_ShouldSavePrompt()
    {
        var context = CreateDapperContext();
        var repository = new CollaborationTaskRepository(context);

        var task = new CollaborationTask
        {
            CollaborationId = 1000,
            Title = $"Test Task with Prompt {Guid.NewGuid()}",
            Description = "Test Description",
            Prompt = "【任务要求】\n1. 积极参与讨论\n2. 提交文档到Git",
            Status = CollaborationTaskStatus.Pending
        };

        var createdTask = await repository.CreateAsync(task);

        Assert.NotNull(createdTask);
        Assert.True(createdTask.Id > 0);
        Assert.Equal(task.Prompt, createdTask.Prompt);

        var retrievedTask = await repository.GetByIdAsync(createdTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.Equal(task.Prompt, retrievedTask.Prompt);
    }

    [Fact]
    public async Task UpdateAsync_WithPrompt_ShouldUpdatePrompt()
    {
        var context = CreateDapperContext();
        var repository = new CollaborationTaskRepository(context);

        var task = new CollaborationTask
        {
            CollaborationId = 1000,
            Title = $"Test Task for Update {Guid.NewGuid()}",
            Description = "Original Description",
            Prompt = "Original Prompt",
            Status = CollaborationTaskStatus.Pending
        };

        var createdTask = await repository.CreateAsync(task);

        createdTask.Title = "Updated Title";
        createdTask.Description = "Updated Description";
        createdTask.Prompt = "【更新后的任务要求】\n- 必须提交文档\n- 必须调用Git工具";

        var updatedTask = await repository.UpdateAsync(createdTask);

        Assert.Equal("Updated Title", updatedTask.Title);
        Assert.Equal("【更新后的任务要求】\n- 必须提交文档\n- 必须调用Git工具", updatedTask.Prompt);

        var retrievedTask = await repository.GetByIdAsync(updatedTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.Equal("【更新后的任务要求】\n- 必须提交文档\n- 必须调用Git工具", retrievedTask.Prompt);
    }

    [Fact]
    public async Task UpdateAsync_WithNullPrompt_ShouldUpdateToNull()
    {
        var context = CreateDapperContext();
        var repository = new CollaborationTaskRepository(context);

        var task = new CollaborationTask
        {
            CollaborationId = 1000,
            Title = $"Test Task for Null Prompt {Guid.NewGuid()}",
            Description = "Test Description",
            Prompt = "Original Prompt",
            Status = CollaborationTaskStatus.Pending
        };

        var createdTask = await repository.CreateAsync(task);
        Assert.Equal("Original Prompt", createdTask.Prompt);

        createdTask.Prompt = null;
        var updatedTask = await repository.UpdateAsync(createdTask);

        Assert.Null(updatedTask.Prompt);

        var retrievedTask = await repository.GetByIdAsync(updatedTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.Null(retrievedTask.Prompt);
    }

    [Fact]
    public async Task CreateAsync_WithoutPrompt_ShouldSaveNullPrompt()
    {
        var context = CreateDapperContext();
        var repository = new CollaborationTaskRepository(context);

        var task = new CollaborationTask
        {
            CollaborationId = 1000,
            Title = $"Test Task without Prompt {Guid.NewGuid()}",
            Description = "Test Description",
            Prompt = null,
            Status = CollaborationTaskStatus.Pending
        };

        var createdTask = await repository.CreateAsync(task);

        Assert.NotNull(createdTask);
        Assert.True(createdTask.Id > 0);
        Assert.Null(createdTask.Prompt);

        var retrievedTask = await repository.GetByIdAsync(createdTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.Null(retrievedTask.Prompt);
    }
}

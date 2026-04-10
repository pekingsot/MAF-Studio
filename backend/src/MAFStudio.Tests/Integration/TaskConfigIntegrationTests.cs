using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MAFStudio.Application.Services;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using MAFStudio.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MAFStudio.Tests.Integration;

public class TaskConfigIntegrationTests
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
    public async Task UpdateTask_WithConfigFromFrontend_ShouldSaveConfig()
    {
        var context = CreateDapperContext();
        var taskRepository = new CollaborationTaskRepository(context);
        var collaborationRepository = new CollaborationRepository(context);
        var collaborationAgentRepository = new CollaborationAgentRepository(context);
        
        var collaborationService = new CollaborationService(
            collaborationRepository,
            collaborationAgentRepository,
            taskRepository);

        var testUserId = 1000000000000002L;
        
        var createdTask = await collaborationService.CreateTaskAsync(
            collaborationId: 1000,
            title: "测试任务-编辑前",
            description: "测试描述",
            userId: testUserId,
            prompt: null,
            gitUrl: null,
            gitBranch: null,
            gitToken: null,
            agentIds: new List<long> { 1000, 1003, 1002, 1001, 1004 },
            config: "{\"workflowType\":\"GroupChat\",\"orchestrationMode\":\"RoundRobin\",\"maxIterations\":10}");

        Console.WriteLine($"✅ 创建任务成功，ID: {createdTask.Id}");
        Console.WriteLine($"   原始Config: {createdTask.Config}");

        var frontendConfig = "{\"workflowType\":\"GroupChat\",\"orchestrationMode\":\"RoundRobin\",\"maxIterations\":10,\"managerAgentId\":1004,\"managerCustomPrompt\":\"# Role\\n你是一个高水平的项目协调专家和讨论主持人。你的职责是引导群组内的专家 Agent（如架构师、程序员、安全专家）协作完成用户任务。\\n\\n# Rules & Principles\\n1. 观察者视角：你主要负责观察对话进展，不要代替专家回答专业问题。\\n2. 动态点名：\\n   - 始终根据当前讨论的最后一条信息，点名最合适的下一位专家。\\n   - 如果刚提出了架构方案，请点名 [安全专家] 或 [程序员] 进行评审。\\n   - 如果出现了意见分歧，请点名 [架构师] 进行最终裁定。\\n3. 任务账本：\\n   - 记录哪些子任务已完成，哪些待处理。\\n   - 每一轮发言后，简要更新当前的进度状态。\\n4. 严禁复读：如果某个 Agent 开始重复之前的话，立即介入并改变讨论方向或点名其他视角。\\n\\n# Termination Criteria\\n- 当所有专家达成共识，且用户任务已完整实现时，请总结最终结论并回复 \\\"TERMINATE\\\"。\\n- 如果发现讨论陷入无法解决的逻辑死循环，请停止讨论并向用户请求进一步指示。\\n\\n# Output Format\\n你的每轮决策请按以下格式输出（仅限你内部思考时使用，不直接发给用户）：\\n【当前状态】：已完成xx，待完成yy\\n【下一位发言人】：[Agent名称]\\n【理由】：为什么选他？\"}";

        var updatedTask = await collaborationService.UpdateTaskAsync(
            taskId: createdTask.Id,
            title: "开发一个微信小程序-产品文档",
            description: "1.实现可以解析抖音,快手,等视频,直接获取高清无水印\n2.用户直接粘贴分享的链接,自动解析内容里的链接\n3.增加广告,每天看一次广告就可以当天无线是使用\n4.广告就接微信小程序的广告\n5.给出一份产品文档,以及市场调研的技术可行性",
            prompt: null,
            gitUrl: null,
            gitBranch: null,
            gitToken: null,
            agentIds: new List<long> { 1000, 1003, 1002, 1001, 1004 },
            config: frontendConfig);

        Console.WriteLine($"✅ 更新任务成功，ID: {updatedTask.Id}");
        Console.WriteLine($"   更新后Config长度: {updatedTask.Config?.Length ?? 0}");

        var retrievedTask = await taskRepository.GetByIdAsync(updatedTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.NotNull(retrievedTask.Config);
        
        Console.WriteLine($"✅ 从数据库取出的Config长度: {retrievedTask.Config.Length}");
        Console.WriteLine($"✅ Config内容前100字符: {retrievedTask.Config.Substring(0, Math.Min(100, retrievedTask.Config.Length))}...");

        var configObj = System.Text.Json.JsonDocument.Parse(retrievedTask.Config);
        var root = configObj.RootElement;

        Assert.True(root.TryGetProperty("managerAgentId", out var managerAgentId));
        Assert.Equal(1004, managerAgentId.GetInt64());
        Console.WriteLine($"✅ managerAgentId: {managerAgentId.GetInt64()}");

        Assert.True(root.TryGetProperty("managerCustomPrompt", out var managerCustomPrompt));
        Assert.Contains("你是一个高水平的项目协调专家", managerCustomPrompt.GetString());
        Console.WriteLine($"✅ managerCustomPrompt长度: {managerCustomPrompt.GetString()?.Length ?? 0}");
    }

    [Fact]
    public async Task CreateTask_WithConfigFromFrontend_ShouldSaveConfig()
    {
        var context = CreateDapperContext();
        var taskRepository = new CollaborationTaskRepository(context);
        var collaborationRepository = new CollaborationRepository(context);
        var collaborationAgentRepository = new CollaborationAgentRepository(context);
        
        var collaborationService = new CollaborationService(
            collaborationRepository,
            collaborationAgentRepository,
            taskRepository);

        var testUserId = 1000000000000002L;

        var config = "{\"workflowType\":\"GroupChat\",\"orchestrationMode\":\"RoundRobin\",\"maxIterations\":10,\"managerAgentId\":1004,\"managerCustomPrompt\":\"# Role\\n你是一个高水平的项目协调专家和讨论主持人。你的职责是引导群组内的专家 Agent（如架构师、程序员、安全专家）协作完成用户任务。\\n\\n# Rules & Principles\\n1. 观察者视角：你主要负责观察对话进展，不要代替专家回答专业问题。\\n2. 动态点名：\\n   - 始终根据当前讨论的最后一条信息，点名最合适的下一位专家。\\n   - 如果刚提出了架构方案，请点名 [安全专家] 或 [程序员] 进行评审。\\n   - 如果出现了意见分歧，请点名 [架构师] 进行最终裁定。\\n3. 任务账本：\\n   - 记录哪些子任务已完成，哪些待处理。\\n   - 每一轮发言后，简要更新当前的进度状态。\\n4. 严禁复读：如果某个 Agent 开始重复之前的话，立即介入并改变讨论方向或点名其他视角。\\n\\n# Termination Criteria\\n- 当所有专家达成共识，且用户任务已完整实现时，请总结最终结论并回复 \\\"TERMINATE\\\"。\\n- 如果发现讨论陷入无法解决的逻辑死循环，请停止讨论并向用户请求进一步指示。\\n\\n# Output Format\\n你的每轮决策请按以下格式输出（仅限你内部思考时使用，不直接发给用户）：\\n【当前状态】：已完成xx，待完成yy\\n【下一位发言人】：[Agent名称]\\n【理由】：为什么选他？\"}";

        Console.WriteLine($"✅ 准备创建任务:");
        Console.WriteLine($"   Title: 开发一个微信小程序-产品文档");
        Console.WriteLine($"   Config长度: {config.Length}");

        var createdTask = await collaborationService.CreateTaskAsync(
            collaborationId: 1000,
            title: "开发一个微信小程序-产品文档",
            description: "1.实现可以解析抖音,快手,等视频,直接获取高清无水印\n2.用户直接粘贴分享的链接,自动解析内容里的链接\n3.增加广告,每天看一次广告就可以当天无线是使用\n4.广告就接微信小程序的广告\n5.给出一份产品文档,以及市场调研的技术可行性",
            userId: testUserId,
            prompt: null,
            gitUrl: null,
            gitBranch: null,
            gitToken: null,
            agentIds: new List<long> { 1000, 1003, 1002, 1001, 1004 },
            config: config);

        Console.WriteLine($"✅ 创建任务成功，ID: {createdTask.Id}");
        Console.WriteLine($"   创建后Config长度: {createdTask.Config?.Length ?? 0}");

        var retrievedTask = await taskRepository.GetByIdAsync(createdTask.Id);
        Assert.NotNull(retrievedTask);
        Assert.NotNull(retrievedTask.Config);
        
        Console.WriteLine($"✅ 从数据库取出的Config长度: {retrievedTask.Config.Length}");
        Console.WriteLine($"✅ Config内容前150字符: {retrievedTask.Config.Substring(0, Math.Min(150, retrievedTask.Config.Length))}...");

        var savedConfigObj = System.Text.Json.JsonDocument.Parse(retrievedTask.Config);
        var savedRoot = savedConfigObj.RootElement;

        Assert.True(savedRoot.TryGetProperty("managerAgentId", out var managerAgentId));
        Assert.Equal(1004, managerAgentId.GetInt64());
        Console.WriteLine($"✅ managerAgentId: {managerAgentId.GetInt64()}");

        Assert.True(savedRoot.TryGetProperty("managerCustomPrompt", out var managerCustomPrompt));
        Assert.Contains("你是一个高水平的项目协调专家", managerCustomPrompt.GetString());
        Console.WriteLine($"✅ managerCustomPrompt长度: {managerCustomPrompt.GetString()?.Length ?? 0}");
    }
}

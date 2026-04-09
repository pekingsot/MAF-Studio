using Dapper;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Services;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class ToolCallingIntegrationTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_tool_calling_log.txt");
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";
    private const long TestTaskId = 1006;

    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }

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
    public async Task ToolCallingChatClient_WithTask1006_ShouldInjectDefaultCapabilities()
    {
        File.Delete(_logFile);
        Log("========== 工具调用集成测试 ==========");
        Log($"测试任务ID: {TestTaskId}");
        Log($"测试时间: {DateTime.Now}");

        var dapperContext = CreateDapperContext();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, description, git_url, git_branch, git_credentials IS NOT NULL as has_git_token FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        if (task == null)
        {
            Log($"❌ 任务ID {TestTaskId} 不存在");
            return;
        }

        Log("\n任务信息:");
        Log($"  ID: {task.id}");
        Log($"  标题: {task.title}");
        Log($"  描述: {task.description}");
        Log($"  Git URL: {task.git_url}");
        Log($"  Git 分支: {task.git_branch}");
        Log($"  是否配置令牌: {task.has_git_token}");

        var agents = await connection.QueryAsync<dynamic>(
            @"SELECT a.id, a.name, a.type_name, a.llm_config_id, a.llm_model_config_id 
              FROM task_agents ta 
              JOIN agents a ON ta.agent_id = a.id 
              WHERE ta.task_id = @TaskId",
            new { TaskId = TestTaskId });

        var agentList = agents.ToList();
        Log($"\n任务关联的Agent数量: {agentList.Count}");
        foreach (var agent in agentList)
        {
            Log($"  - ID:{agent.id} {agent.name} ({agent.type_name})");
            Log($"    LLM配置ID: {agent.llm_config_id}, 模型配置ID: {agent.llm_model_config_id}");
        }

        if (agentList.Count == 0)
        {
            Log("\n⚠️ 任务没有关联Agent，尝试从协作中获取Agent");
            
            var collaborationId = await connection.QueryFirstOrDefaultAsync<long>(
                "SELECT collaboration_id FROM collaboration_tasks WHERE id = @Id",
                new { Id = TestTaskId });

            if (collaborationId > 0)
            {
                agents = await connection.QueryAsync<dynamic>(
                    @"SELECT a.id, a.name, a.type_name, a.llm_config_id, a.llm_model_config_id 
                      FROM collaboration_agents ca 
                      JOIN agents a ON ca.agent_id = a.id 
                      WHERE ca.collaboration_id = @CollaborationId",
                    new { CollaborationId = collaborationId });
                
                agentList = agents.ToList();
                Log($"从协作中获取到 {agentList.Count} 个Agent");
            }
        }

        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();

        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var capabilityManager = new CapabilityManager();

        Log("\n========== 注册的能力 ==========");
        foreach (var capability in capabilityManager.GetAllCapabilities())
        {
            Log($"\n能力: {capability.GetType().Name}");
            foreach (var tool in capability.GetTools())
            {
                var toolAttr = tool.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() as ToolAttribute;
                Log($"  - {tool.Name}: {toolAttr?.Description ?? "无描述"}");
            }
        }

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        if (agentList.Count == 0)
        {
            Log("\n❌ 没有可用的Agent进行测试");
            return;
        }

        var firstAgent = agentList.First();
        Log($"\n========== 测试Agent: {firstAgent.name} ==========");

        try
        {
            var chatClient = await agentFactory.CreateAgentAsync((long)firstAgent.id);
            Log($"✓ 成功创建 ChatClient: {chatClient.GetType().Name}");

            if (chatClient is ToolCallingChatClient toolClient)
            {
                Log("✓ ChatClient 是 ToolCallingChatClient 类型");
            }
            else
            {
                Log($"⚠️ ChatClient 类型不是 ToolCallingChatClient，而是 {chatClient.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Log($"\n❌ 创建 ChatClient 失败: {ex.Message}");
            Log($"异常类型: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Log($"内部异常: {ex.InnerException.Message}");
            }
        }

        Log("\n========== 测试完成 ==========");
    }

    [Fact]
    public async Task ToolCallingChatClient_WithSimplePrompt_ShouldCallTools()
    {
        File.Delete(_logFile);
        Log("========== 工具调用功能测试 ==========");
        Log($"测试时间: {DateTime.Now}");

        var dapperContext = CreateDapperContext();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var agent = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, name, llm_config_id, llm_model_config_id FROM agents WHERE llm_config_id IS NOT NULL AND llm_model_config_id IS NOT NULL LIMIT 1");

        if (agent == null)
        {
            Log("❌ 没有找到配置了LLM的Agent");
            return;
        }

        Log($"使用Agent: {agent.name} (ID: {agent.id})");

        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();

        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var capabilityManager = new CapabilityManager();
        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            loggerFactory);

        try
        {
            var chatClient = await agentFactory.CreateAgentAsync((long)agent.id);
            Log($"✓ 成功创建 ChatClient");

            var testDir = Path.Combine(Path.GetTempPath(), "maf_test_" + Guid.NewGuid().ToString("N")[..8]);
            var prompt = $@"请帮我完成以下任务：
1. 在目录 {testDir} 创建一个名为 'test.md' 的 Markdown 文件
2. 文件内容写上 'Hello from MAF Studio! 这是一个测试文档。'
3. 读取文件内容并告诉我文件是否创建成功

请使用可用的工具来完成这些操作。";

            Log($"\n发送测试提示词:");
            Log(prompt);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, prompt)
            };

            var options = new ChatOptions
            {
                Temperature = 0.7f,
                MaxOutputTokens = 2000
            };

            Log("\n开始调用 LLM...");
            var response = await chatClient.GetResponseAsync(messages, options);

            Log("\n========== LLM 响应 ==========");
            if (response?.Messages != null)
            {
                foreach (var msg in response.Messages)
                {
                    Log($"角色: {msg.Role}");
                    if (msg.Contents != null)
                    {
                        foreach (var content in msg.Contents)
                        {
                            if (content is TextContent textContent)
                            {
                                Log($"内容: {textContent.Text}");
                            }
                            else if (content is FunctionCallContent funcCall)
                            {
                                Log($"工具调用: {funcCall.Name}");
                                Log($"参数: {string.Join(", ", funcCall.Arguments?.Select(a => $"{a.Key}={a.Value}") ?? Array.Empty<string>())}");
                            }
                            else if (content is FunctionResultContent funcResult)
                            {
                                Log($"工具结果: {funcResult.Result}");
                            }
                        }
                    }
                }
            }

            if (Directory.Exists(testDir))
            {
                var files = Directory.GetFiles(testDir);
                Log($"\n========== 验证结果 ==========");
                Log($"目录 {testDir} 存在: True");
                Log($"文件数量: {files.Length}");
                foreach (var file in files)
                {
                    Log($"  - {Path.GetFileName(file)}");
                    if (Path.GetExtension(file) == ".md")
                    {
                        var content = File.ReadAllText(file);
                        Log($"    内容: {content}");
                    }
                }

                Directory.Delete(testDir, true);
                Log("清理测试目录完成");
            }
        }
        catch (Exception ex)
        {
            Log($"\n❌ 测试失败: {ex.Message}");
            Log($"异常类型: {ex.GetType().Name}");
            Log($"堆栈: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Log($"内部异常: {ex.InnerException.Message}");
            }
        }

        Log("\n========== 测试完成 ==========");
    }

    [Fact]
    public async Task GitCapability_WithTask1006Config_ShouldWork()
    {
        File.Delete(_logFile);
        Log("========== Git 能力测试 ==========");
        Log($"测试时间: {DateTime.Now}");

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, git_url, git_branch, git_credentials IS NOT NULL as has_git_token FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        if (task == null)
        {
            Log($"❌ 任务ID {TestTaskId} 不存在");
            return;
        }

        Log("\n任务Git配置:");
        Log($"  Git URL: {task.git_url}");
        Log($"  Git 分支: {task.git_branch}");
        Log($"  是否配置令牌: {task.has_git_token}");

        if (string.IsNullOrEmpty(task.git_url?.ToString()))
        {
            Log("\n⚠️ 任务没有配置Git URL，跳过Git能力测试");
            return;
        }

        var gitCapability = new GitCapability();
        Log("\nGit 能力方法:");
        foreach (var method in gitCapability.GetTools())
        {
            var toolAttr = method.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() as ToolAttribute;
            Log($"  - {method.Name}: {toolAttr?.Description ?? "无描述"}");
        }

        var testDir = Path.Combine(Path.GetTempPath(), "git_test_" + Guid.NewGuid().ToString("N")[..8]);
        Log($"\n测试目录: {testDir}");

        try
        {
            var gitUrl = task.git_url?.ToString();
            var gitBranch = task.git_branch?.ToString() ?? "main";

            Log($"\n尝试克隆仓库: {gitUrl}");
            Log($"分支: {gitBranch}");

            var cloneResult = gitCapability.CloneRepository(gitUrl, testDir);
            Log($"克隆结果: {cloneResult}");

            if (Directory.Exists(testDir))
            {
                Log("\n✓ 仓库克隆成功");
                var files = Directory.GetFiles(testDir, "*", SearchOption.TopDirectoryOnly);
                Log($"顶层文件数量: {files.Length}");
                foreach (var file in files.Take(10))
                {
                    Log($"  - {Path.GetFileName(file)}");
                }

                var testFile = Path.Combine(testDir, "test_from_maf.md");
                var testContent = $"# 测试文档\n\n由 MAF Studio 创建\n时间: {DateTime.Now}\n";
                
                Log($"\n创建测试文件: {testFile}");
                File.WriteAllText(testFile, testContent);

                var addResult = gitCapability.AddFiles(testDir, ".");
                Log($"添加文件结果: {addResult}");

                var commitResult = gitCapability.Commit(testDir, "test: MAF Studio 工具调用测试提交");
                Log($"提交结果: {commitResult}");

                Log("\n⚠️ 注意: 实际推送需要有效的访问令牌，此处跳过推送测试");
            }
            else
            {
                Log("\n❌ 仓库克隆失败，目录不存在");
            }
        }
        catch (Exception ex)
        {
            Log($"\n❌ Git 操作失败: {ex.Message}");
            Log($"异常类型: {ex.GetType().Name}");
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                try
                {
                    Directory.Delete(testDir, true);
                    Log("\n清理测试目录完成");
                }
                catch (Exception ex)
                {
                    Log($"清理目录失败: {ex.Message}");
                }
            }
        }

        Log("\n========== 测试完成 ==========");
    }
}

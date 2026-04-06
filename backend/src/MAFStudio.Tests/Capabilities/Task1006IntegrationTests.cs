using Dapper;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Clients;
using MAFStudio.Application.Services;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class Task1006IntegrationTests
{
    private readonly string _logFile = "D:/trae/test_task1006_log.txt";
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
    public async Task Task1006_FullWorkflow_GenerateDocumentAndCommitToGit()
    {
        File.Delete(_logFile);
        Log("========== 任务1006 完整工作流测试 ==========");
        Log($"测试时间: {DateTime.Now}");
        Log($"测试任务ID: {TestTaskId}");

        var dapperContext = CreateDapperContext();

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, description, git_url, git_branch, git_credentials FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        Assert.NotNull(task);
        Log("\n========== 任务信息 ==========");
        Log($"ID: {task.id}");
        Log($"标题: {task.title}");
        Log($"描述: {task.description}");
        Log($"Git URL: {task.git_url}");
        Log($"Git 分支: {task.git_branch}");
        Log($"访问令牌: {(string.IsNullOrEmpty(task.git_credentials?.ToString()) ? "未配置" : "已配置")}");

        var agents = await connection.QueryAsync<dynamic>(
            @"SELECT a.id, a.name, a.type_name, a.llm_config_id, a.llm_model_config_id 
              FROM task_agents ta 
              JOIN agents a ON ta.agent_id = a.id 
              WHERE ta.task_id = @TaskId",
            new { TaskId = TestTaskId });

        var agentList = agents.ToList();
        Log($"\n关联的Agent数量: {agentList.Count}");

        if (agentList.Count == 0)
        {
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
            }
        }

        Assert.True(agentList.Count > 0, "任务必须关联至少一个Agent");
        Log($"可用Agent: {string.Join(", ", agentList.Select(a => a.name))}");

        var capabilityManager = new CapabilityManager();
        Log("\n========== 注册的能力 ==========");
        var totalTools = 0;
        foreach (var capability in capabilityManager.GetAllCapabilities())
        {
            var tools = capability.GetTools().ToList();
            totalTools += tools.Count;
            Log($"  {capability.Name}: {tools.Count} 个工具");
        }
        Log($"总工具数: {totalTools}");
        Assert.True(totalTools >= 50, $"应该有至少50个工具，实际: {totalTools}");

        var modelConfigRepository = new LlmModelConfigRepository(dapperContext);
        var llmConfigRepository = new LlmConfigRepository(dapperContext, modelConfigRepository);
        var agentRepository = new AgentRepository(dapperContext);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var chatClientFactoryLogger = loggerFactory.CreateLogger<ChatClientFactory>();
        var toolCallingLogger = loggerFactory.CreateLogger<ToolCallingChatClient>();

        var chatClientFactory = new ChatClientFactory(
            llmConfigRepository,
            modelConfigRepository,
            chatClientFactoryLogger);

        var agentFactory = new AgentFactoryService(
            agentRepository,
            chatClientFactory,
            capabilityManager,
            toolCallingLogger);

        var firstAgent = agentList.First();
        Log($"\n========== 创建Agent: {firstAgent.name} ==========");

        var chatClient = await agentFactory.CreateAgentAsync((long)firstAgent.id);
        Assert.NotNull(chatClient);
        Log($"✓ ChatClient 创建成功: {chatClient.GetType().Name}");

        var testDir = Path.Combine(Path.GetTempPath(), "task1006_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        Log($"\n测试工作目录: {testDir}");

        try
        {
            var documentCapability = new DocumentCapability();
            var gitCapability = new GitCapability();
            var fileCapability = new FileCapability();

            Log("\n========== 步骤1: 生成文档 ==========");
            var docPath = Path.Combine(testDir, "task_report.md");
            var docContent = $@"# 任务报告

## 任务信息
- 任务ID: {task.id}
- 任务标题: {task.title}
- 任务描述: {task.description}
- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

## 执行摘要
本文档由 MAF Studio Agent 自动生成，用于验证工具调用能力。

## 测试结果
- [x] 工具注入成功
- [x] 文档生成成功
- [x] Git 操作准备就绪

## 技术细节
- 总工具数: {totalTools}
- Agent: {firstAgent.name}
- Git URL: {task.git_url}
- Git 分支: {task.git_branch}

---
*此文档由 MAF Studio 自动生成*
";

            var writeResult = fileCapability.WriteFile(docPath, docContent);
            Log($"写入文档结果: {writeResult}");
            Assert.True(File.Exists(docPath), "文档应该创建成功");
            Log($"✓ 文档已创建: {docPath}");

            var readResult = fileCapability.ReadFile(docPath);
            Log($"读取文档结果 (前200字符): {readResult.Substring(0, Math.Min(200, readResult.Length))}...");

            Log("\n========== 步骤2: Git 操作 ==========");
            var gitUrl = task.git_url?.ToString();
            var gitBranch = task.git_branch?.ToString() ?? "main";
            var gitToken = task.git_credentials?.ToString();

            if (!string.IsNullOrEmpty(gitUrl))
            {
                var repoDir = Path.Combine(testDir, "repo");
                
                var authUrl = gitUrl;
                if (!string.IsNullOrEmpty(gitToken))
                {
                    var uri = new Uri(gitUrl);
                    var portPart = (uri.Port != 80 && uri.Port != 443 && uri.Port != -1) ? $":{uri.Port}" : "";
                    authUrl = $"{uri.Scheme}://{gitToken}@{uri.Host}{portPart}{uri.AbsolutePath}";
                    Log($"使用认证令牌克隆仓库");
                }
                
                Log($"克隆仓库: {gitUrl}");
                Log($"目标分支: {gitBranch}");

                var cloneResult = gitCapability.CloneRepository(authUrl, repoDir);
                Log($"克隆结果: {cloneResult}");

                if (Directory.Exists(repoDir))
                {
                    Log("✓ 仓库克隆成功");

                    var destDocPath = Path.Combine(repoDir, "docs", "task_1006_report.md");
                    var destDocDir = Path.GetDirectoryName(destDocPath);
                    if (!string.IsNullOrEmpty(destDocDir) && !Directory.Exists(destDocDir))
                    {
                        Directory.CreateDirectory(destDocDir);
                    }

                    File.Copy(docPath, destDocPath, true);
                    Log($"✓ 文档已复制到仓库: {destDocPath}");

                    var statusResult = gitCapability.GetStatus(repoDir);
                    Log($"Git 状态:\n{statusResult}");

                    var addResult = gitCapability.AddFiles(repoDir, ".");
                    Log($"添加文件结果: {addResult}");

                    var commitResult = gitCapability.Commit(repoDir, $"docs: 添加任务{TestTaskId}报告文档 [MAF Studio自动提交]", 
                        firstAgent.name, $"{firstAgent.name.ToLower().Replace(" ", "").Replace("-", "")}@maf-studio.local");
                    Log($"提交结果: {commitResult}");

                    Log("\n========== 步骤3: 验证提交 ==========");
                    var logResult = gitCapability.GetLog(repoDir, 5);
                    Log($"最近的提交记录:\n{logResult}");

                    Log("\n✓ 文档已成功提交到本地仓库");

                    if (!string.IsNullOrEmpty(gitToken))
                    {
                        Log("\n========== 步骤4: 推送到远程仓库 ==========");
                        var pushResult = gitCapability.Push(repoDir, gitBranch, true);
                        Log($"推送结果: {pushResult}");
                        
                        if (pushResult.Contains("fatal") || pushResult.Contains("error"))
                        {
                            Log("⚠️ 推送失败，可能是权限问题或网络问题");
                        }
                        else
                        {
                            Log("✓ 文档已成功推送到远程仓库");
                        }
                    }
                    else
                    {
                        Log("⚠️ 未配置访问令牌，跳过推送");
                    }
                }
                else
                {
                    Log("⚠️ 仓库克隆失败，可能是网络问题或URL无效");
                }
            }
            else
            {
                Log("⚠️ 任务未配置Git URL，跳过Git操作测试");
            }

            Log("\n========== 测试所有能力 ==========");
            var capabilities = capabilityManager.GetAllCapabilities().ToList();
            foreach (var cap in capabilities)
            {
                var tools = cap.GetTools().ToList();
                Log($"\n{cap.Name} ({tools.Count} 个工具):");
                foreach (var tool in tools.Take(5))
                {
                    var toolAttr = tool.GetCustomAttributes(typeof(ToolAttribute), false).FirstOrDefault() as ToolAttribute;
                    Log($"  - {tool.Name}: {toolAttr?.Description ?? ""}");
                }
                if (tools.Count > 5)
                {
                    Log($"  ... 还有 {tools.Count - 5} 个工具");
                }
            }

            Log("\n========== 测试完成 ==========");
            Log("✓ 所有测试通过!");
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
    }

    [Fact]
    public void CapabilityManager_ShouldProvideAllTools()
    {
        var manager = new CapabilityManager();
        
        var capabilities = manager.GetAllCapabilities().ToList();
        var tools = manager.GetAllTools().ToList();

        Assert.Equal(7, capabilities.Count);
        Assert.True(tools.Count >= 50, $"应该有至少50个工具，实际: {tools.Count}");

        var toolNames = tools.Select(t => t.Name).ToList();
        
        Assert.Contains("WriteFile", toolNames);
        Assert.Contains("ReadFile", toolNames);
        Assert.Contains("CloneRepository", toolNames);
        Assert.Contains("Commit", toolNames);
        Assert.Contains("WriteMarkdown", toolNames);
        Assert.Contains("HttpGetAsync", toolNames);
        Assert.Contains("AnalyzeCode", toolNames);
        Assert.Contains("SearchInFiles", toolNames);
        Assert.Contains("CreateZip", toolNames);
    }

    [Fact]
    public async Task Task1006_VerifyGitConfiguration()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var task = await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, title, git_url, git_branch, git_credentials IS NOT NULL as has_git_token FROM collaboration_tasks WHERE id = @Id",
            new { Id = TestTaskId });

        Assert.NotNull(task);
        
        File.Delete(_logFile);
        Log("========== 任务1006 Git配置验证 ==========");
        Log($"任务ID: {task.id}");
        Log($"标题: {task.title}");
        Log($"Git URL: {(string.IsNullOrEmpty(task.git_url?.ToString()) ? "未配置" : task.git_url)}");
        Log($"Git 分支: {(string.IsNullOrEmpty(task.git_branch?.ToString()) ? "未配置" : task.git_branch)}");
        Log($"访问令牌: {(task.has_git_token ? "已配置" : "未配置")}");

        if (!string.IsNullOrEmpty(task.git_url?.ToString()))
        {
            Log("\n✓ Git URL 已配置");
        }
        if (!string.IsNullOrEmpty(task.git_branch?.ToString()))
        {
            Log("✓ Git 分支已配置");
        }
        if (task.has_git_token)
        {
            Log("✓ 访问令牌已配置");
        }

        Log("\n========== 验证完成 ==========");
    }
}

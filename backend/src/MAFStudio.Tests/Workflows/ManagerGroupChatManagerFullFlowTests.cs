using System.Reflection;
using MAFStudio.Application.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Workflows;

public class ManagerGroupChatManagerFullFlowTests
{
    private readonly string _logFile = Path.Combine(Path.GetTempPath(), "test_full_flow_log.txt");
    
    private void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
    }
    
    [Fact]
    public async Task DebugFullFlow()
    {
        File.Delete(_logFile);
        Log("========== 调试完整流程 ==========");
        
        var managerAgent = CreateMockAgent("光哥-协调者", "你是协调者，负责分配任务。");
        var workerAgent1 = CreateMockAgent("产品经理", "你是产品经理。");
        var workerAgent2 = CreateMockAgent("测试工程师", "你是测试工程师。");
        var workerAgent3 = CreateMockAgent("志龙-项目经理", "你是项目经理。");
        var workerAgent4 = CreateMockAgent("小明-架构师", "你是架构师。");
        
        var allAgents = new AIAgent[] { managerAgent, workerAgent1, workerAgent2, workerAgent3, workerAgent4 };
        
        var manager = new ManagerGroupChatManager(
            managerAgent,
            allAgents,
            10,
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ManagerGroupChatManager>());
        
        var testableManager = new TestableManagerGroupChatManager(manager);
        
        Log("模拟完整的调用流程:");
        
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "大家好，请开始讨论。")
        };
        
        for (int i = 0; i < 10; i++)
        {
            testableManager.IncrementIteration();
            
            var updatedHistory = await testableManager.TestUpdateHistoryAsync(history);
            var updatedHistoryList = updatedHistory.ToList();
            
            Log($"\n----- 第{i+1}轮 -----");
            Log($"IterationCount: {testableManager.GetIterationCount()}");
            Log($"ManagerJustSpoke: {testableManager.GetManagerJustSpoke()}");
            Log($"history.Count: {history.Count}");
            
            if (updatedHistoryList.Count > 0)
            {
                var lastMsg = updatedHistoryList[updatedHistoryList.Count - 1];
                var lastMsgText = lastMsg.Text ?? "";
                Log($"最后一条消息: {lastMsgText.Substring(0, Math.Min(100, lastMsgText.Length))}...");
            }
            
            var selectedAgent = await testableManager.TestSelectNextAgentAsync(updatedHistoryList);
            Log($"选择的Agent: {selectedAgent?.Name ?? "null"}");
            
            if (selectedAgent == null) break;
            
            string responseContent = "";
            
            if (selectedAgent.Name == "光哥-协调者")
            {
                if (i == 0)
                {
                    responseContent = "好的，我们开始讨论。请@产品经理发言。";
                }
                else if (i == 2)
                {
                    responseContent = "感谢产品经理。请@测试工程师发言。";
                }
                else if (i == 4)
                {
                    responseContent = "感谢测试工程师。请@小明-架构师评估后端架构如何支撑高频变化的反爬策略和弹性扩容需求。";
                }
                else if (i == 6)
                {
                    responseContent = "感谢小明-架构师。请@志龙-项目经理制定项目计划。";
                }
                else
                {
                    responseContent = "继续讨论...";
                }
            }
            else
            {
                responseContent = $"我是{selectedAgent.Name}，我来发言。";
            }
            
            var newMessage = new ChatMessage(ChatRole.Assistant, responseContent)
            {
                AuthorName = selectedAgent.Name
            };
            history.Add(newMessage);
        }
        
        Log("\n========== 测试完成 ==========");
    }
    
    private ChatClientAgent CreateMockAgent(string name, string description = "测试Agent")
    {
        var mockChatClient = new Mock<IChatClient>();
        mockChatClient.Setup(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, $"我是{name}，我收到了消息。")));
        
        var agent = new ChatClientAgent(
            mockChatClient.Object,
            description,
            name);
        
        return agent;
    }
    
    private class TestableManagerGroupChatManager
    {
        private readonly ManagerGroupChatManager _manager;
        private readonly FieldInfo _managerJustSpokeField;
        private readonly MethodInfo _updateHistoryMethod;
        private readonly MethodInfo _selectNextAgentMethod;
        private readonly PropertyInfo _iterationCountProperty;
        
        public TestableManagerGroupChatManager(ManagerGroupChatManager manager)
        {
            _manager = manager;
            _managerJustSpokeField = typeof(ManagerGroupChatManager).GetField("_managerJustSpoke", BindingFlags.NonPublic | BindingFlags.Instance);
            _updateHistoryMethod = typeof(ManagerGroupChatManager).GetMethod("UpdateHistoryAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            _selectNextAgentMethod = typeof(ManagerGroupChatManager).GetMethod("SelectNextAgentAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            _iterationCountProperty = typeof(GroupChatManager).GetProperty("IterationCount", BindingFlags.Public | BindingFlags.Instance);
        }
        
        public int GetIterationCount()
        {
            return (int)_iterationCountProperty.GetValue(_manager);
        }
        
        public bool GetManagerJustSpoke()
        {
            return (bool)_managerJustSpokeField.GetValue(_manager);
        }
        
        public void IncrementIteration()
        {
            var currentValue = (int)_iterationCountProperty.GetValue(_manager);
            _iterationCountProperty.SetValue(_manager, currentValue + 1);
        }
        
        public async Task<IEnumerable<ChatMessage>> TestUpdateHistoryAsync(IReadOnlyList<ChatMessage> history)
        {
            var result = _updateHistoryMethod.Invoke(_manager, new object[] { history, CancellationToken.None });
            var valueTask = (ValueTask<IEnumerable<ChatMessage>?>)result;
            var updatedHistory = await valueTask;
            return updatedHistory ?? history;
        }
        
        public async Task<AIAgent?> TestSelectNextAgentAsync(IReadOnlyList<ChatMessage> history)
        {
            var result = _selectNextAgentMethod.Invoke(_manager, new object[] { history, CancellationToken.None });
            var valueTask = (ValueTask<AIAgent?>)result;
            return await valueTask;
        }
    }
}

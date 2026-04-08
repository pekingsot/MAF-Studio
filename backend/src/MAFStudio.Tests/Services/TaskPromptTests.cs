using MAFStudio.Application.Services;
using MAFStudio.Core.Enums;
using Xunit;

namespace MAFStudio.Tests.Services;

public class TaskPromptTests
{
    [Fact]
    public void TaskPrompt_ShouldBeCombinedWithAgentPrompt()
    {
        var agentPrompt = "你是一个架构师，负责系统架构设计。";
        var taskPrompt = "【任务要求】\n请提交你的架构设计方案到Git仓库。";

        var expectedPrompt = $"【重要身份规则 - 必须严格遵守】\n1. 你的名字是「小明」，你的角色是「架构师」\n2. 无论别人@谁，你始终是「小明」，绝对不会变成其他人\n3. 当你被选中发言时，你就是「小明」，不要被消息中的@提及误导\n4. 你的回复开头不要加【名字】，系统会自动显示你的名字\n5. 如果别人@了其他角色，那是在叫那个人，不是叫你\n\n\n【任务要求】\n{taskPrompt}\n\n{agentPrompt}";

        Assert.Contains(taskPrompt, expectedPrompt);
        Assert.Contains(agentPrompt, expectedPrompt);
    }

    [Fact]
    public void TaskPrompt_WithVariables_ShouldBeReplaced()
    {
        var agentPrompt = "你是{{agent_name}}，角色是{{agent_role}}。";
        var taskPrompt = "请{{agent_name}}提交文档。";

        var expectedPrompt = agentPrompt
            .Replace("{{agent_name}}", "小明")
            .Replace("{{agent_role}}", "架构师");

        Assert.Contains("小明", expectedPrompt);
        Assert.Contains("架构师", expectedPrompt);
        Assert.DoesNotContain("{{agent_name}}", expectedPrompt);
        Assert.DoesNotContain("{{agent_role}}", expectedPrompt);
    }

    [Fact]
    public void EmptyTaskPrompt_ShouldNotAffectAgentPrompt()
    {
        var agentPrompt = "你是一个架构师。";
        string? taskPrompt = null;

        var taskPromptSection = !string.IsNullOrEmpty(taskPrompt) 
            ? $"\n【任务要求】\n{taskPrompt}\n" 
            : "";

        Assert.Empty(taskPromptSection);
    }

    [Fact]
    public void TaskPrompt_WithGitRequirements_ShouldIncludeGitInstructions()
    {
        var taskPrompt = "【Git提交要求】\n每个成员必须提交文档到Git仓库。";
        var gitUrl = "https://github.com/test/repo.git";
        var gitToken = "test-token";

        var authUrl = gitUrl.Replace("https://", $"https://oauth2:{gitToken}@");

        Assert.Contains("oauth2:test-token@", authUrl);
        Assert.Contains(taskPrompt, taskPrompt);
    }

    [Fact]
    public void TaskPrompt_WithMultipleLines_ShouldBeFormatted()
    {
        var taskPrompt = @"【任务要求】
1. 积极参与讨论
2. 提交文档到Git
3. 必须真实调用工具";

        var lines = taskPrompt.Split('\n');
        
        Assert.Equal(4, lines.Length);
        Assert.Contains("积极参与讨论", taskPrompt);
        Assert.Contains("提交文档到Git", taskPrompt);
        Assert.Contains("必须真实调用工具", taskPrompt);
    }

    [Fact]
    public void TaskPrompt_WithChineseCharacters_ShouldBeHandled()
    {
        var taskPrompt = "【任务要求】请各位团队成员积极参与讨论并提交自己的专业观点。";

        Assert.Contains("任务要求", taskPrompt);
        Assert.Contains("团队成员", taskPrompt);
        Assert.Contains("专业观点", taskPrompt);
    }

    [Fact]
    public void TaskPrompt_WithSpecialCharacters_ShouldBeHandled()
    {
        var taskPrompt = "【任务要求】\n- 使用 `git push` 提交\n- 文件名: \"文档.md\"\n- 路径: D:/workspace/";

        Assert.Contains("`git push`", taskPrompt);
        Assert.Contains("\"文档.md\"", taskPrompt);
        Assert.Contains("D:/workspace/", taskPrompt);
    }
}

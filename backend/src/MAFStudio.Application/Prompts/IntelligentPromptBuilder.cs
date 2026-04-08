namespace MAFStudio.Application.Prompts;

public class IntelligentPromptBuilder : BaseSystemPromptBuilder
{
    protected override string BuildModeInstruction()
    {
        return @"
【智能调度规则】
- 当前是智能调度模式，由AI根据讨论内容选择最合适的发言者
- 轮到你发言时，直接发表你的观点
- 根据讨论进展，适时贡献你的专业见解
";
    }
}

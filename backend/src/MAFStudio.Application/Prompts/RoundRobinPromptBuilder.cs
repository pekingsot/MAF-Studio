namespace MAFStudio.Application.Prompts;

public class RoundRobinPromptBuilder : BaseSystemPromptBuilder
{
    protected override string BuildModeInstruction()
    {
        return @"
【轮询发言规则】
- 当前是轮询模式，所有成员按顺序轮流发言
- 轮到你发言时，直接发表你的观点，不需要等待任何人点名
- 积极参与讨论，主动贡献你的专业见解
";
    }
}

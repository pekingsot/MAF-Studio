namespace MAFStudio.Application.Prompts;

public class ManagerPromptBuilder : BaseSystemPromptBuilder
{
    protected override string BuildModeInstruction()
    {
        return @"
【协调者点名规则】
- 当前是协调者模式，由协调者（Manager）点名安排发言
- 只有被协调者@点名后才能发言
- 如果没有被点名，请保持安静等待
";
    }
}

namespace MAFStudio.Application.Clients;

public class ModelCallFailedException : Exception
{
    public IReadOnlyList<ModelFailureDetail> FailedModels { get; }

    public ModelCallFailedException(string message, IReadOnlyList<ModelFailureDetail> failedModels)
        : base(message)
    {
        FailedModels = failedModels;
    }

    public ModelCallFailedException(string message, IReadOnlyList<ModelFailureDetail> failedModels, Exception innerException)
        : base(message, innerException)
    {
        FailedModels = failedModels;
    }

    public string GetUserFriendlyMessage()
    {
        if (FailedModels.Count == 0)
            return Message;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("⚠️ 模型调用失败，以下模型均无法响应：");
        sb.AppendLine();

        foreach (var model in FailedModels)
        {
            var reason = GetShortReason(model.ErrorMessage);
            sb.AppendLine($"  • {model.ModelName}：{reason}");
        }

        sb.AppendLine();
        sb.AppendLine("可能原因：");
        sb.AppendLine("  1. 免费额度已耗尽，请在管理后台关闭\"仅使用免费额度\"模式");
        sb.AppendLine("  2. 模型服务暂时不可用，请稍后重试");
        sb.AppendLine("  3. API Key 配置有误，请检查模型配置");

        return sb.ToString();
    }

    private static string GetShortReason(string errorMessage)
    {
        if (errorMessage.Contains("FreeTierOnly", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("free tier", StringComparison.OrdinalIgnoreCase))
            return "免费额度已耗尽";

        if (errorMessage.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            return "API Key 无效或已过期";

        if (errorMessage.Contains("403", StringComparison.OrdinalIgnoreCase))
            return "访问被拒绝（额度不足或权限不够）";

        if (errorMessage.Contains("429", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            return "请求频率超限，请稍后重试";

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("timed out", StringComparison.OrdinalIgnoreCase))
            return "请求超时";

        if (errorMessage.Contains("connection", StringComparison.OrdinalIgnoreCase))
            return "网络连接失败";

        return errorMessage.Length > 80 ? errorMessage.Substring(0, 80) + "..." : errorMessage;
    }
}

public class ModelFailureDetail
{
    public string ModelName { get; }
    public string ErrorMessage { get; }
    public int Priority { get; }

    public ModelFailureDetail(string modelName, string errorMessage, int priority)
    {
        ModelName = modelName;
        ErrorMessage = errorMessage;
        Priority = priority;
    }
}

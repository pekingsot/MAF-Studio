using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface ILlmConfigService
{
    Task<List<LlmConfig>> GetByUserIdAsync(string userId);
    Task<List<LlmConfig>> GetAllAsync();
    Task<LlmConfig?> GetByIdAsync(long id);
    Task<LlmConfig> CreateAsync(string name, string provider, string? apiKey, string? endpoint, string? defaultModel, string userId);
    Task<LlmConfig> UpdateAsync(long id, string name, string? apiKey, string? endpoint, string? defaultModel);
    Task<bool> DeleteAsync(long id);
    Task SetDefaultAsync(long id, string userId);
    Task<ConnectionTestResult> TestConnectionAsync(long id);
    Task<ConnectionTestResult> TestModelConnectionAsync(long configId, long modelId);
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
}

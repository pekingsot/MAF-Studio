using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface ISystemLogService
{
    Task LogAsync(string level, string category, string message, string? exception = null, string? stackTrace = null, string? userId = null, string? requestPath = null, string? additionalData = null);
    Task<List<SystemLog>> GetAsync(string? level = null, string? category = null, string? userId = null, int limit = 100);
    Task<bool> DeleteAsync(long id);
}

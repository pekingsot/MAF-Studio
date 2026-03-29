using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface ISystemLogService
{
    Task LogAsync(string level, string category, string message, string? exception = null, string? stackTrace = null, long? userId = null, string? requestPath = null, string? additionalData = null);
    Task<List<SystemLog>> GetAsync(string? level = null, string? category = null, long? userId = null, int limit = 100);
    Task<bool> DeleteAsync(long id);
}

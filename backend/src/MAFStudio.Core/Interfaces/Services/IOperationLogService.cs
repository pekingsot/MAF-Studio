using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Services;

public interface IOperationLogService
{
    Task LogAsync(long userId, string action, string resourceType, string? description, string? details);
    
    Task LogApiCallAsync(
        long userId,
        string action,
        string resourceType,
        string? description,
        string? details,
        string? ipAddress,
        string? userAgent,
        string? requestPath,
        string? requestMethod,
        int? statusCode,
        long? durationMs,
        string? errorMessage);

    Task<List<OperationLog>> GetByUserIdAsync(long? userId = null, int limit = 100);
    Task<List<OperationLog>> GetAllAsync(int limit = 100);
}

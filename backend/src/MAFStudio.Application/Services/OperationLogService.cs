using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace MAFStudio.Application.Services;

public class OperationLogService : IOperationLogService
{
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<OperationLogService> _logger;

    public OperationLogService(IOperationLogRepository logRepository, ILogger<OperationLogService> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task LogAsync(long userId, string action, string resourceType, string? description, string? details)
    {
        try
        {
            var log = new OperationLog
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                Description = description,
                Details = details,
            };

            await _logRepository.CreateAsync(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存操作日志失败: {UserId}, {Action}, {ResourceType}", userId, action, resourceType);
        }
    }

    public async Task LogApiCallAsync(
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
        string? errorMessage)
    {
        try
        {
            var log = new OperationLog
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                Description = description,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RequestPath = requestPath,
                RequestMethod = requestMethod,
                StatusCode = statusCode,
                DurationMs = durationMs,
                ErrorMessage = errorMessage,
            };

            await _logRepository.CreateAsync(log);
            _logger.LogInformation("API调用日志已保存: {Method} {Path} - {StatusCode} ({Duration}ms)", requestMethod, requestPath, statusCode, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存API调用日志失败: {UserId}, {Action}, {ResourceType}", userId, action, resourceType);
        }
    }

    public async Task<List<OperationLog>> GetByUserIdAsync(long? userId = null, int limit = 100)
    {
        if (!userId.HasValue)
        {
            return await _logRepository.GetAllAsync(limit);
        }
        return await _logRepository.GetByUserIdAsync(userId.Value, limit);
    }

    public async Task<List<OperationLog>> GetAllAsync(int limit = 100)
    {
        return await _logRepository.GetAllAsync(limit);
    }
}

using Microsoft.Extensions.Logging;
using MAFStudio.Backend.Data;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 数据库日志记录器
    /// 将日志写入数据库
    /// </summary>
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentQueue<SystemLog> _logQueue;
        private static readonly ConcurrentQueue<SystemLog> _sharedQueue = new();
        private static Timer? _flushTimer;
        private static readonly object _lock = new();

        public DatabaseLogger(string categoryName, IServiceProvider serviceProvider)
        {
            _categoryName = categoryName;
            _serviceProvider = serviceProvider;
            _logQueue = _sharedQueue;

            lock (_lock)
            {
                if (_flushTimer == null)
                {
                    _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            var httpContext = _serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext;
            var user = httpContext?.User;
            var userId = user?.FindFirst("sub")?.Value ?? user?.FindFirst("id")?.Value ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = user?.FindFirst("name")?.Value ?? user?.Identity?.Name;

            var log = new SystemLog
            {
                Id = Guid.NewGuid(),
                Level = logLevel.ToString(),
                Category = _categoryName,
                Message = message,
                Exception = exception?.ToString(),
                StackTrace = exception?.StackTrace,
                RequestPath = httpContext?.Request?.Path,
                RequestMethod = httpContext?.Request?.Method,
                UserId = userId,
                UserName = userName,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            if (state is IReadOnlyList<KeyValuePair<string, object?>> stateList)
            {
                foreach (var item in stateList)
                {
                    if (item.Key == "RequestPath")
                    {
                        log.RequestPath = item.Value?.ToString();
                    }
                    else if (item.Key == "RequestMethod")
                    {
                        log.RequestMethod = item.Value?.ToString();
                    }
                    else if (item.Key == "UserId")
                    {
                        log.UserId = item.Value?.ToString();
                    }
                    else if (item.Key == "UserName")
                    {
                        log.UserName = item.Value?.ToString();
                    }
                    else if (item.Key == "IpAddress")
                    {
                        log.IpAddress = item.Value?.ToString();
                    }
                }
            }

            _logQueue.Enqueue(log);
        }

        private static void FlushLogs(object? state)
        {
            lock (_lock)
            {
                if (_sharedQueue.IsEmpty)
                {
                    return;
                }

                var logsToWrite = new List<SystemLog>();
                while (_sharedQueue.TryDequeue(out var log))
                {
                    logsToWrite.Add(log);
                }

                if (logsToWrite.Count == 0)
                {
                    return;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = Program.ServiceProvider?.CreateScope();
                        var dbContext = scope?.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        if (dbContext != null)
                        {
                            dbContext.SystemLogs.AddRange(logsToWrite);
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                    }
                });
            }
        }
    }

    /// <summary>
    /// 数据库日志记录器提供者
    /// </summary>
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, DatabaseLogger> _loggers = new();

        public DatabaseLoggerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new DatabaseLogger(name, _serviceProvider));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    /// <summary>
    /// 数据库日志扩展方法
    /// </summary>
    public static class DatabaseLoggerExtensions
    {
        public static ILoggingBuilder AddDatabaseLogger(this ILoggingBuilder builder, IServiceProvider serviceProvider)
        {
            builder.AddProvider(new DatabaseLoggerProvider(serviceProvider));
            return builder;
        }
    }
}

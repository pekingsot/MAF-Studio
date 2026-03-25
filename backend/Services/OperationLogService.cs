using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 操作日志服务实现
    /// 提供操作日志的记录和查询功能
    /// </summary>
    public class OperationLogService : IOperationLogService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public OperationLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        public async Task LogAsync(string userId, string operation, string module, string? description = null, string? requestData = null, string? ipAddress = null, bool isSuccess = true, string? errorMessage = null, long? duration = null)
        {
            var log = new OperationLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Operation = operation,
                Module = module,
                Description = description,
                RequestData = requestData,
                IpAddress = ipAddress,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Duration = duration,
                CreatedAt = DateTime.UtcNow
            };

            _context.OperationLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 获取日志列表
        /// </summary>
        public async Task<List<OperationLog>> GetLogsAsync(string userId, bool isAdmin, int page = 1, int pageSize = 50, string? module = null, string? operation = null)
        {
            var query = _context.OperationLogs.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(ol => ol.UserId == userId);
            }

            if (!string.IsNullOrEmpty(module))
            {
                query = query.Where(ol => ol.Module == module);
            }

            if (!string.IsNullOrEmpty(operation))
            {
                query = query.Where(ol => ol.Operation == operation);
            }

            return await query
                .AsNoTracking()
                .OrderByDescending(ol => ol.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}

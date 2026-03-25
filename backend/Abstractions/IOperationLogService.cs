using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 操作日志服务接口
    /// 提供操作日志的记录和查询功能
    /// </summary>
    public interface IOperationLogService
    {
        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="operation">操作类型</param>
        /// <param name="module">模块名称</param>
        /// <param name="description">操作描述</param>
        /// <param name="requestData">请求数据</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="duration">执行时长（毫秒）</param>
        Task LogAsync(string userId, string operation, string module, string? description = null, string? requestData = null, string? ipAddress = null, bool isSuccess = true, string? errorMessage = null, long? duration = null);

        /// <summary>
        /// 获取日志列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="isAdmin">是否为管理员</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="module">模块筛选</param>
        /// <param name="operation">操作筛选</param>
        /// <returns>日志列表</returns>
        Task<List<OperationLog>> GetLogsAsync(string userId, bool isAdmin, int page = 1, int pageSize = 50, string? module = null, string? operation = null);
    }
}

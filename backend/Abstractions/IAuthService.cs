using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 认证服务接口
    /// 提供用户注册、登录、权限验证等功能
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="email">邮箱</param>
        /// <param name="password">密码</param>
        /// <returns>注册结果（是否成功、消息、用户实体、JWT令牌）</returns>
        Task<(bool Success, string Message, User? User, string? Token)> RegisterAsync(string username, string email, string password);

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>登录结果（是否成功、消息、用户实体、JWT令牌）</returns>
        Task<(bool Success, string Message, User? User, string? Token)> LoginAsync(string username, string password);

        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户实体，不存在返回null</returns>
        Task<User?> GetUserByIdAsync(string userId);

        /// <summary>
        /// 检查用户是否为管理员
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否为管理员</returns>
        Task<bool> IsAdminAsync(string userId);
    }
}

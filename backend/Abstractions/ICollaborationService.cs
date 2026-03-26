using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Abstractions
{
    /// <summary>
    /// 协作服务接口
    /// 提供协作管理、任务分配、智能体协作等功能
    /// </summary>
    public interface ICollaborationService
    {
        /// <summary>
        /// 获取所有协作
        /// </summary>
        /// <param name="userId">用户ID，为空则获取所有</param>
        /// <returns>协作列表</returns>
        Task<List<Collaboration>> GetAllCollaborationsAsync(string? userId = null);

        /// <summary>
        /// 根据用户ID获取协作列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>协作列表</returns>
        Task<List<Collaboration>> GetCollaborationsByUserIdAsync(string userId);

        /// <summary>
        /// 根据ID获取协作
        /// </summary>
        /// <param name="id">协作ID</param>
        /// <param name="userId">用户ID，用于权限验证</param>
        /// <returns>协作实体，不存在返回null</returns>
        Task<Collaboration?> GetCollaborationByIdAsync(Guid id, string? userId = null);

        /// <summary>
        /// 创建协作
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="path">工作路径</param>
        /// <param name="gitRepositoryUrl">Git仓库地址</param>
        /// <param name="gitBranch">Git分支</param>
        /// <param name="gitUsername">Git用户名</param>
        /// <param name="gitEmail">Git邮箱</param>
        /// <param name="gitAccessToken">Git访问令牌</param>
        /// <param name="userId">用户ID</param>
        /// <returns>创建的协作实体</returns>
        Task<Collaboration> CreateCollaborationAsync(
            string name,
            string? description,
            string? path,
            string? gitRepositoryUrl = null,
            string? gitBranch = null,
            string? gitUsername = null,
            string? gitEmail = null,
            string? gitAccessToken = null,
            string? userId = null);

        /// <summary>
        /// 更新协作
        /// </summary>
        /// <param name="id">协作ID</param>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="path">工作路径</param>
        /// <param name="gitRepositoryUrl">Git仓库地址</param>
        /// <param name="gitBranch">Git分支</param>
        /// <param name="gitUsername">Git用户名</param>
        /// <param name="gitEmail">Git邮箱</param>
        /// <param name="gitAccessToken">Git访问令牌</param>
        /// <param name="userId">用户ID</param>
        /// <returns>更新后的协作实体，不存在返回null</returns>
        Task<Collaboration?> UpdateCollaborationAsync(
            Guid id,
            string name,
            string? description,
            string? path,
            string? gitRepositoryUrl = null,
            string? gitBranch = null,
            string? gitUsername = null,
            string? gitEmail = null,
            string? gitAccessToken = null,
            string? userId = null);

        /// <summary>
        /// 删除协作
        /// </summary>
        /// <param name="id">协作ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteCollaborationAsync(Guid id, string? userId = null);

        /// <summary>
        /// 将智能体添加到协作
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <param name="agentId">智能体ID</param>
        /// <param name="role">角色</param>
        /// <param name="userId">用户ID</param>
        /// <returns>更新后的协作实体，不存在返回null</returns>
        Task<Collaboration?> AddAgentToCollaborationAsync(Guid collaborationId, Guid agentId, string? role, string? userId = null);

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <param name="title">任务标题</param>
        /// <param name="description">任务描述</param>
        /// <param name="userId">用户ID</param>
        /// <returns>创建的任务实体</returns>
        Task<CollaborationTask?> CreateTaskAsync(Guid collaborationId, string title, string? description, string? userId = null);

        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="status">新状态</param>
        /// <param name="userId">用户ID</param>
        /// <returns>更新后的任务实体，不存在返回null</returns>
        Task<CollaborationTask?> UpdateTaskStatusAsync(Guid taskId, Data.TaskStatus status, string? userId = null);

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteTaskAsync(Guid taskId, string? userId = null);

        /// <summary>
        /// 从协作中移除智能体
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <param name="agentId">智能体ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>是否移除成功</returns>
        Task<bool> RemoveAgentFromCollaborationAsync(Guid collaborationId, Guid agentId, string? userId = null);

        /// <summary>
        /// 获取协作项目的智能体列表
        /// </summary>
        /// <param name="collaborationId">协作ID</param>
        /// <returns>智能体列表</returns>
        Task<List<CollaborationAgent>?> GetCollaborationAgentsAsync(Guid collaborationId);
    }
}

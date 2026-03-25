namespace MAFStudio.Backend.Models.Requests
{
    /// <summary>
    /// 创建协作请求
    /// </summary>
    public class CreateCollaborationRequest
    {
        /// <summary>
        /// 协作名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 协作描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 工作路径
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Git仓库地址
        /// </summary>
        public string? GitRepositoryUrl { get; set; }

        /// <summary>
        /// Git分支
        /// </summary>
        public string? GitBranch { get; set; }

        /// <summary>
        /// Git用户名
        /// </summary>
        public string? GitUsername { get; set; }

        /// <summary>
        /// Git邮箱
        /// </summary>
        public string? GitEmail { get; set; }

        /// <summary>
        /// Git访问令牌
        /// </summary>
        public string? GitAccessToken { get; set; }
    }

    /// <summary>
    /// 添加智能体到协作请求
    /// </summary>
    public class AddAgentRequest
    {
        /// <summary>
        /// 智能体ID
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        public string? Role { get; set; }
    }

    /// <summary>
    /// 创建任务请求
    /// </summary>
    public class CreateTaskRequest
    {
        /// <summary>
        /// 任务标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新任务状态请求
    /// </summary>
    public class UpdateTaskStatusRequest
    {
        /// <summary>
        /// 任务状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}

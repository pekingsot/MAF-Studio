namespace MAFStudio.Backend.Models.DTOs
{
    /// <summary>
    /// 数据传输对象基类
    /// DTO (Data Transfer Object) - 用于服务间数据传输
    /// </summary>
    public abstract class BaseDto
    {
    }

    /// <summary>
    /// 用户消息 DTO
    /// </summary>
    public class UserMessageDto : BaseDto
    {
        public Guid Id { get; set; }
        public string? FromAgentId { get; set; }
        public string FromAgentName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 智能体开始响应 DTO
    /// </summary>
    public class AgentStartDto : BaseDto
    {
        public Guid MessageId { get; set; }
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 智能体流式内容块 DTO
    /// </summary>
    public class AgentChunkDto : BaseDto
    {
        public Guid MessageId { get; set; }
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 智能体响应完成 DTO
    /// </summary>
    public class AgentEndDto : BaseDto
    {
        public Guid MessageId { get; set; }
        public Guid SavedMessageId { get; set; }
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string FullContent { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    /// <summary>
    /// 智能体错误 DTO
    /// </summary>
    public class AgentErrorDto : BaseDto
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完成信号 DTO
    /// </summary>
    public class DoneDto : BaseDto
    {
        public Guid CollaborationId { get; set; }
    }

    /// <summary>
    /// 错误消息 DTO
    /// </summary>
    public class ErrorDto : BaseDto
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// API 响应包装 DTO
    /// </summary>
    public class ApiResponseDto<T> : BaseDto where T : class
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponseDto<T> Ok(T data, string? message = null)
        {
            return new ApiResponseDto<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponseDto<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponseDto<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}

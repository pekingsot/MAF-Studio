using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models.Requests;
using MAFStudio.Backend.Models.DTOs;
using MAFStudio.Backend.Models.VOs;
using MAFStudio.Backend.Models.Mappers;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MAFStudio.Backend.Hubs;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CollaborationsController : ControllerBase
    {
        private readonly ICollaborationService _collaborationService;
        private readonly IAgentRuntimeService _agentRuntimeService;
        private readonly IMessageService _messageService;
        private readonly IAgentService _agentService;
        private readonly IHubContext<AgentHub> _hubContext;
        private readonly ILogger<CollaborationsController> _logger;

        public CollaborationsController(
            ICollaborationService collaborationService,
            IAgentRuntimeService agentRuntimeService,
            IMessageService messageService,
            IAgentService agentService,
            IHubContext<AgentHub> hubContext,
            ILogger<CollaborationsController> logger)
        {
            _collaborationService = collaborationService;
            _agentRuntimeService = agentRuntimeService;
            _messageService = messageService;
            _agentService = agentService;
            _hubContext = hubContext;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<ActionResult<List<CollaborationVo>>> GetAllCollaborations()
        {
            var userId = GetUserId();
            var collaborations = await _collaborationService.GetAllCollaborationsAsync(userId);
            var vos = collaborations.Select(c => 
            {
                var agents = c.Agents?.ToList() ?? new List<CollaborationAgent>();
                var tasks = c.Tasks?.ToList() ?? new List<CollaborationTask>();
                return c.ToVo(agents, tasks);
            }).ToList();
            return Ok(vos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollaborationVo>> GetCollaboration(Guid id)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.GetCollaborationByIdAsync(id, userId);
            if (collaboration == null)
            {
                return NotFound();
            }
            var agents = await _collaborationService.GetCollaborationAgentsAsync(id);
            var tasks = collaboration.Tasks?.ToList() ?? new List<CollaborationTask>();
            return Ok(collaboration.ToVo(agents, tasks));
        }

        [HttpPost]
        public async Task<ActionResult<Collaboration>> CreateCollaboration([FromBody] CreateCollaborationRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.CreateCollaborationAsync(
                request.Name,
                request.Description,
                request.Path,
                request.GitRepositoryUrl,
                request.GitBranch,
                request.GitUsername,
                request.GitEmail,
                request.GitAccessToken,
                userId
            );
            return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaboration);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Collaboration>> UpdateCollaboration(Guid id, [FromBody] CreateCollaborationRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.UpdateCollaborationAsync(
                id,
                request.Name,
                request.Description,
                request.Path,
                request.GitRepositoryUrl,
                request.GitBranch,
                request.GitUsername,
                request.GitEmail,
                request.GitAccessToken,
                userId
            );
            if (collaboration == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return Ok(collaboration);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCollaboration(Guid id)
        {
            var userId = GetUserId();
            var result = await _collaborationService.DeleteCollaborationAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "协作项目不存在或无权限删除" });
            }
            return NoContent();
        }

        [HttpPost("{id}/agents")]
        public async Task<ActionResult<Collaboration>> AddAgentToCollaboration(Guid id, [FromBody] AddAgentRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.AddAgentToCollaborationAsync(
                id,
                request.AgentId,
                request.Role,
                userId
            );
            if (collaboration == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return Ok(collaboration);
        }

        [HttpDelete("{id}/agents/{agentId}")]
        public async Task<ActionResult> RemoveAgentFromCollaboration(Guid id, Guid agentId)
        {
            var userId = GetUserId();
            var result = await _collaborationService.RemoveAgentFromCollaborationAsync(id, agentId, userId);
            if (!result)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return NoContent();
        }

        [HttpPost("{id}/tasks")]
        public async Task<ActionResult<CollaborationTask>> CreateTask(Guid id, [FromBody] CreateTaskRequest request)
        {
            var userId = GetUserId();
            var task = await _collaborationService.CreateTaskAsync(id, request.Title, request.Description, userId);
            if (task == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return CreatedAtAction(nameof(GetCollaboration), new { id }, task);
        }

        [HttpPatch("tasks/{taskId}/status")]
        public async Task<ActionResult<CollaborationTask>> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var userId = GetUserId();
            if (!Enum.TryParse<Data.TaskStatus>(request.Status, out var status))
            {
                return BadRequest(new { message = "无效的任务状态" });
            }
            var task = await _collaborationService.UpdateTaskStatusAsync(taskId, status, userId);
            if (task == null)
            {
                return NotFound(new { message = "任务不存在或无权限修改" });
            }
            return Ok(task);
        }

        [HttpDelete("tasks/{taskId}")]
        public async Task<ActionResult> DeleteTask(Guid taskId)
        {
            var userId = GetUserId();
            var result = await _collaborationService.DeleteTaskAsync(taskId, userId);
            if (!result)
            {
                return NotFound(new { message = "任务不存在或无权限删除" });
            }
            return NoContent();
        }

        /// <summary>
        /// 发送协作消息 - 支持群聊模式和@提及
        /// 用户消息会广播给协作中的所有智能体
        /// 如果使用@提及，只有被提及的智能体会回复
        /// </summary>
        [HttpPost("{id}/chat")]
        public async Task<ActionResult> SendCollaborationMessage(Guid id, [FromBody] SendCollaborationMessageRequest request)
        {
            var userId = GetUserId();
            
            _logger.LogInformation("=== 协作聊天请求开始 ===");
            _logger.LogInformation("协作ID: {CollaborationId}, 用户ID: {UserId}, 消息内容: {Content}", 
                id, userId, request.Content);
            _logger.LogInformation("提及的智能体ID: {MentionedAgentIds}", 
                request.MentionedAgentIds != null ? string.Join(",", request.MentionedAgentIds) : "无");
            
            var collaboration = await _collaborationService.GetCollaborationByIdAsync(id, userId);
            if (collaboration == null)
            {
                _logger.LogWarning("协作项目不存在或无权限访问: {CollaborationId}", id);
                return NotFound(new { message = "协作项目不存在或无权限访问" });
            }

            _logger.LogInformation("找到协作项目: {CollaborationName}", collaboration.Name);

            var collaborationAgents = await _collaborationService.GetCollaborationAgentsAsync(id);
            if (collaborationAgents == null || collaborationAgents.Count == 0)
            {
                _logger.LogWarning("协作项目中没有智能体: {CollaborationId}", id);
                return BadRequest(new { message = "协作项目中没有智能体" });
            }

            _logger.LogInformation("协作项目中有 {Count} 个智能体: {Agents}", 
                collaborationAgents.Count, 
                string.Join(", ", collaborationAgents.Select(ca => ca.Agent?.Name ?? "未知")));

            var mentionedAgentIds = request.MentionedAgentIds ?? new List<Guid>();
            
            _logger.LogInformation("保存用户消息到数据库...");
            AgentMessage userMessage;
            try
            {
                userMessage = await _messageService.SendCollaborationMessageAsync(
                    request.Content,
                    id,
                    mentionedAgentIds.Count > 0 ? mentionedAgentIds : null
                );
                _logger.LogInformation("用户消息保存成功, 消息ID: {MessageId}", userMessage.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户消息失败");
                return BadRequest(new { message = $"保存消息失败: {ex.Message}" });
            }

            var targetAgents = mentionedAgentIds.Count > 0
                ? collaborationAgents.Where(ca => mentionedAgentIds.Contains(ca.AgentId)).ToList()
                : collaborationAgents;

            _logger.LogInformation("目标智能体数量: {Count}, 模式: {Mode}", 
                targetAgents.Count, mentionedAgentIds.Count > 0 ? "@提及模式" : "广播模式");

            var responses = new List<object>();
            var errors = new List<string>();

            foreach (var collabAgent in targetAgents)
            {
                var agentId = collabAgent.AgentId;
                var agent = collabAgent.Agent;
                
                if (agent == null)
                {
                    _logger.LogWarning("智能体为空，跳过: {AgentId}", agentId);
                    continue;
                }

                try
                {
                    _logger.LogInformation(">>> 开始处理智能体 {AgentName}({AgentId})", agent.Name, agentId);

                    var otherAgents = collaborationAgents
                        .Where(a => a.AgentId != agentId)
                        .Select(a => a.Agent?.Name ?? "未知")
                        .ToList();

                    var systemPrompt = mentionedAgentIds.Count > 0
                        ? $"你是{agent.Name}，一个{agent.Type}类型的智能体。用户在群聊中@了你，请直接回复用户的问题。"
                        : $"你是{agent.Name}，一个{agent.Type}类型的智能体。你正在参与一个协作群聊，协作名称是{collaboration.Name}。" +
                          (otherAgents.Count > 0 ? $"协作中还有其他智能体：{string.Join("、", otherAgents)}。" : "") +
                          $"请根据用户的消息给出你的回应。";

                    _logger.LogInformation("调用智能体运行时...");
                    var response = await _agentRuntimeService.ExecuteAsync(agentId, $"{systemPrompt}\n\n用户消息：{request.Content}");
                    _logger.LogInformation("智能体 {AgentName} 响应成功，响应长度: {Length}", agent.Name, response?.Length ?? 0);

                    var agentMessage = await _messageService.SendAgentResponseAsync(agentId, response, id);
                    _logger.LogInformation("智能体响应消息已保存，消息ID: {MessageId}", agentMessage.Id);

                    responses.Add(new
                    {
                        agentId = agentId,
                        agentName = agent.Name,
                        content = response,
                        timestamp = agentMessage.CreatedAt.ToString("O")
                    });

                    await _hubContext.Clients.Group($"Collaboration_{id}")
                        .SendAsync("ReceiveMessage", agentMessage.ToVo());
                    
                    _logger.LogInformation("<<< 智能体 {AgentName} 处理完成", agent.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "协作 {CollaborationId} 智能体 {AgentId}({AgentName}) 响应失败", id, agentId, agent.Name);
                    errors.Add($"{agent.Name}: {ex.Message}");
                }
            }

            _logger.LogInformation("=== 协作聊天请求完成，成功响应: {SuccessCount}，失败: {ErrorCount} ===", 
                responses.Count, errors.Count);

            return Ok(new
            {
                success = true,
                userMessage = new
                {
                    id = userMessage.Id,
                    fromAgentId = "user",
                    fromAgentName = "用户",
                    content = request.Content,
                    type = "text",
                    timestamp = userMessage.CreatedAt,
                    collaborationId = id,
                    mentionedAgents = mentionedAgentIds.Count > 0 
                        ? collaborationAgents.Where(ca => mentionedAgentIds.Contains(ca.AgentId)).Select(ca => ca.Agent?.Name).ToList()
                        : null
                },
                agentResponses = responses,
                errors = errors.Count > 0 ? errors : null
            });
        }

        /// <summary>
        /// 流式协作聊天 - 支持 SSE 流式输出
        /// </summary>
        [HttpPost("{id}/chat/stream")]
        public async Task StreamCollaborationMessage(Guid id, [FromBody] SendCollaborationMessageRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetUserId();
                
                Response.Headers.Append("Content-Type", "text/event-stream");
                Response.Headers.Append("Cache-Control", "no-cache");
                Response.Headers.Append("Connection", "keep-alive");

                _logger.LogInformation("开始流式协作聊天 - CollaborationId: {CollaborationId}, UserId: {UserId}", id, userId);

                var collaboration = await _collaborationService.GetCollaborationByIdAsync(id, userId);
                if (collaboration == null)
                {
                    _logger.LogWarning("协作项目不存在或无权限访问 - CollaborationId: {CollaborationId}", id);
                    await SendEventAsync("error", new ErrorDto { Message = "协作项目不存在或无权限访问" });
                    return;
                }

                _logger.LogInformation("协作项目信息 - Id: {Id}, Name: {Name}, Status: {Status}", 
                    collaboration.Id, collaboration.Name, collaboration.Status);

                var collaborationAgents = await _collaborationService.GetCollaborationAgentsAsync(id);
                if (collaborationAgents == null || collaborationAgents.Count == 0)
                {
                    _logger.LogWarning("协作项目中没有智能体 - CollaborationId: {CollaborationId}", id);
                    await SendEventAsync("error", new ErrorDto { Message = "协作项目中没有智能体" });
                    return;
                }

                _logger.LogInformation("找到 {Count} 个智能体", collaborationAgents.Count);
                
                foreach (var ca in collaborationAgents)
                {
                    _logger.LogInformation("智能体 - AgentId: {AgentId}, AgentName: {AgentName}, AgentType: {AgentType}", 
                        ca.AgentId, ca.Agent?.Name, ca.Agent?.Type);
                }

                var mentionedAgentIds = request.MentionedAgentIds ?? new List<Guid>();
                
                _logger.LogInformation("准备保存用户消息 - Content: {Content}, MentionedAgentIds: {Ids}", 
                    request.Content, string.Join(",", mentionedAgentIds));
                
                var userMessage = await _messageService.SendCollaborationMessageAsync(
                    request.Content,
                    id,
                    mentionedAgentIds.Count > 0 ? mentionedAgentIds : null
                );

                _logger.LogInformation("用户消息保存成功 - MessageId: {MessageId}, CreatedAt: {CreatedAt}", 
                    userMessage.Id, userMessage.CreatedAt);

                var userMessageDto = new UserMessageDto
                {
                    Id = userMessage.Id,
                    FromAgentId = null,
                    FromAgentName = "用户",
                    Content = request.Content,
                    Type = "text",
                    Timestamp = userMessage.CreatedAt.ToString("O"),
                    SenderType = "User"
                };

                _logger.LogInformation("准备发送 userMessage 事件 - DTO: {DtoJson}", 
                    System.Text.Json.JsonSerializer.Serialize(userMessageDto));

                await SendEventAsync("userMessage", userMessageDto);

                var targetAgents = mentionedAgentIds.Count > 0
                    ? collaborationAgents.Where(ca => mentionedAgentIds.Contains(ca.AgentId)).ToList()
                    : collaborationAgents;

                foreach (var collabAgent in targetAgents)
                {
                    var agentId = collabAgent.AgentId;
                    var agent = collabAgent.Agent;
                
                if (agent == null) continue;

                try
                {
                    var otherAgents = collaborationAgents
                        .Where(a => a.AgentId != agentId)
                        .Select(a => a.Agent?.Name ?? "未知")
                        .ToList();

                    var systemPrompt = mentionedAgentIds.Count > 0
                        ? $"你是{agent.Name}，一个{agent.Type}类型的智能体。用户在群聊中@了你，请直接回复用户的问题。"
                        : $"你是{agent.Name}，一个{agent.Type}类型的智能体。你正在参与一个协作群聊，协作名称是{collaboration.Name}。" +
                          (otherAgents.Count > 0 ? $"协作中还有其他智能体：{string.Join("、", otherAgents)}。" : "") +
                          $"请根据用户的消息给出你的回应。";

                    var fullContent = new System.Text.StringBuilder();
                    var messageGuid = Guid.NewGuid();

                    await SendEventAsync("agentStart", new AgentStartDto
                    {
                        MessageId = messageGuid,
                        AgentId = agentId,
                        AgentName = agent.Name
                    });

                    await foreach (var chunk in _agentRuntimeService.ExecuteStreamAsync(agentId, $"{systemPrompt}\n\n用户消息：{request.Content}", cancellationToken))
                    {
                        fullContent.Append(chunk);
                        await SendEventAsync("agentChunk", new AgentChunkDto
                        {
                            MessageId = messageGuid,
                            AgentId = agentId,
                            AgentName = agent.Name,
                            Content = chunk
                        });
                    }

                    var finalContent = fullContent.ToString();
                    var agentMessage = await _messageService.SendAgentResponseAsync(agentId, finalContent, id);

                    await SendEventAsync("agentEnd", new AgentEndDto
                    {
                        MessageId = messageGuid,
                        SavedMessageId = agentMessage.Id,
                        AgentId = agentId,
                        AgentName = agent.Name,
                        FullContent = finalContent,
                        Timestamp = agentMessage.CreatedAt.ToString("O")
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "协作 {CollaborationId} 智能体 {AgentId}({AgentName}) 流式响应失败", id, agentId, agent.Name);
                    await SendEventAsync("agentError", new AgentErrorDto
                    {
                        AgentId = agentId,
                        AgentName = agent.Name,
                        Error = ex.Message
                    });
                }
            }

            await SendEventAsync("done", new DoneDto
            {
                CollaborationId = id
            });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "流式协作聊天失败 - CollaborationId: {CollaborationId}", id);
                await SendEventAsync("error", new ErrorDto
                {
                    Message = $"服务器错误: {ex.Message}"
                });
            }
        }

        private async Task SendEventAsync<T>(string eventType, T data) where T : class
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                var json = System.Text.Json.JsonSerializer.Serialize(data, options);
                await Response.WriteAsync($"event: {eventType}\n");
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "序列化事件失败 - EventType: {EventType}, DataType: {DataType}", 
                    eventType, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// A2A通信 - 智能体之间的消息传递
        /// </summary>
        [HttpPost("{id}/a2a")]
        public async Task<ActionResult> SendA2AMessage(Guid id, [FromBody] A2AMessageRequest request)
        {
            var userId = GetUserId();
            
            var collaboration = await _collaborationService.GetCollaborationByIdAsync(id, userId);
            if (collaboration == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限访问" });
            }

            var fromAgent = await _agentService.GetAgentByIdAsync(request.FromAgentId);
            var toAgent = await _agentService.GetAgentByIdAsync(request.ToAgentId);
            
            if (fromAgent == null || toAgent == null)
            {
                return NotFound(new { message = "智能体不存在" });
            }

            try
            {
                _logger.LogInformation("A2A: 智能体 {FromAgentId}({FromName}) -> {ToAgentId}({ToName}): {Message}", 
                    request.FromAgentId, fromAgent.Name, request.ToAgentId, toAgent.Name, request.Content);

                var a2aPrompt = $"你是{toAgent.Name}，收到了来自{fromAgent.Name}的消息。" +
                    $"请根据收到的消息给出你的回应。\n\n来自{fromAgent.Name}的消息：{request.Content}";

                var response = await _agentRuntimeService.ExecuteAsync(request.ToAgentId, a2aPrompt);

                var message = await _messageService.SendMessageAsync(
                    request.FromAgentId,
                    request.ToAgentId,
                    request.Content,
                    MessageType.Command
                );
                message.CollaborationId = id;

                var responseMessage = await _messageService.SendMessageAsync(
                    request.ToAgentId,
                    request.FromAgentId,
                    response,
                    MessageType.Response
                );
                responseMessage.CollaborationId = id;

                await _hubContext.Clients.Group($"Collaboration_{id}")
                    .SendAsync("ReceiveA2AMessage", new
                    {
                        fromAgentId = request.FromAgentId,
                        fromAgentName = fromAgent.Name,
                        toAgentId = request.ToAgentId,
                        toAgentName = toAgent.Name,
                        content = request.Content,
                        response = response,
                        timestamp = DateTime.UtcNow,
                        collaborationId = id
                    });

                return Ok(new
                {
                    success = true,
                    message = new
                    {
                        fromAgentId = request.FromAgentId,
                        fromAgentName = fromAgent.Name,
                        toAgentId = request.ToAgentId,
                        toAgentName = toAgent.Name,
                        content = request.Content,
                        response = response,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A2A通信失败");
                return BadRequest(new { message = $"A2A通信失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取协作项目的智能体列表
        /// </summary>
        [HttpGet("{id}/agents")]
        public async Task<ActionResult<List<CollaborationAgent>>> GetCollaborationAgents(Guid id)
        {
            var userId = GetUserId();
            
            var agents = await _collaborationService.GetCollaborationAgentsAsync(id);
            if (agents == null)
            {
                return NotFound(new { message = "协作项目不存在" });
            }
            
            return Ok(agents);
        }
    }

    public class SendCollaborationMessageRequest
    {
        public string Content { get; set; } = string.Empty;
        public List<Guid>? MentionedAgentIds { get; set; }
    }

    public class A2AMessageRequest
    {
        public Guid FromAgentId { get; set; }
        public Guid ToAgentId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

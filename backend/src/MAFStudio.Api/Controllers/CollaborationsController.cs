using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using MAFStudio.Application.DTOs.Requests;
using MAFStudio.Application.Interfaces;
using System.Security.Claims;
using MAFStudio.Core.Enums;
using MAFStudio.Api.Extensions;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Core.Entities;
using Microsoft.Extensions.AI;
using MAFStudio.Application.Skills;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollaborationsController : ControllerBase
{
    private readonly ICollaborationService _collaborationService;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;
    private readonly IGroupMessageRepository _groupMessageRepository;
    private readonly ICollaborationTaskRepository _collaborationTaskRepository;
    private readonly IEmailService _emailService;
    private readonly IAgentFactoryService _agentFactoryService;
    private readonly IAgentRepository _agentRepository;
    private readonly ICollaborationAgentRepository _collaborationAgentRepository;
    private readonly SkillLoader _skillLoader;
    private readonly ILogger<CollaborationsController> _logger;

    public CollaborationsController(
        ICollaborationService collaborationService, 
        IAuthService authService, 
        IOperationLogService logService,
        IGroupMessageRepository groupMessageRepository,
        ICollaborationTaskRepository collaborationTaskRepository,
        IEmailService emailService,
        IAgentFactoryService agentFactoryService,
        IAgentRepository agentRepository,
        ICollaborationAgentRepository collaborationAgentRepository,
        SkillLoader skillLoader,
        ILogger<CollaborationsController> logger)
    {
        _collaborationService = collaborationService;
        _authService = authService;
        _logService = logService;
        _groupMessageRepository = groupMessageRepository;
        _collaborationTaskRepository = collaborationTaskRepository;
        _emailService = emailService;
        _agentFactoryService = agentFactoryService;
        _agentRepository = agentRepository;
        _collaborationAgentRepository = collaborationAgentRepository;
        _skillLoader = skillLoader;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CollaborationVo>>> GetAllCollaborations()
    {
        var userId = User.GetUserId();
        var collaborations = await _collaborationService.GetByUserIdAsync(userId);
        var vos = collaborations.Select(c => c.ToVo()).ToList();
        return Ok(vos);
    }

    [HttpGet("{id}/agents")]
    public async Task<ActionResult<List<CollaborationAgentVo>>> GetCollaborationAgents(long id)
    {
        var agents = await _collaborationService.GetAgentsWithDetailsAsync(id);
        var vos = agents?.Select(a => new CollaborationAgentVo
        {
            AgentId = a.AgentId,
            AgentName = a.AgentName,
            AgentType = a.AgentType,
            AgentStatus = a.AgentStatus,
            AgentAvatar = a.AgentAvatar,
            Role = a.Role,
            CustomPrompt = a.CustomPrompt,
            SystemPrompt = a.SystemPrompt,
            JoinedAt = a.JoinedAt
        }).ToList() ?? new List<CollaborationAgentVo>();
        
        return Ok(vos);
    }

    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<List<CollaborationTaskVo>>> GetCollaborationTasks(long id)
    {
        var tasks = await _collaborationService.GetTasksAsync(id);
        var vos = tasks?.Select(t => new CollaborationTaskVo
        {
            Id = t.Id,
            CollaborationId = t.CollaborationId,
            Title = t.Title,
            Description = t.Description,
            Prompt = t.Prompt,
            Status = t.Status,
            AssignedTo = t.AssignedTo,
            GitUrl = t.GitUrl,
            GitBranch = t.GitBranch,
            GitToken = t.GitCredentials,
            Config = t.Config,
            TaskFlow = t.TaskFlow,
            CompletedAt = t.CompletedAt,
            CreatedAt = t.CreatedAt
        }).ToList() ?? new List<CollaborationTaskVo>();
        
        return Ok(vos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollaborationVo>> GetCollaboration(long id)
    {
        var userId = User.GetUserId();
        var collaboration = await _collaborationService.GetByIdAsync(id, userId);
        if (collaboration == null)
        {
            return NotFound();
        }

        var vo = collaboration.ToVo();
        var agents = await _collaborationService.GetAgentsWithDetailsAsync(id);
        vo.Agents = agents?.Select(a => new CollaborationAgentVo
        {
            AgentId = a.AgentId,
            AgentName = a.AgentName,
            AgentType = a.AgentType,
            AgentStatus = a.AgentStatus,
            AgentAvatar = a.AgentAvatar,
            Role = a.Role,
            CustomPrompt = a.CustomPrompt,
            SystemPrompt = a.SystemPrompt,
            JoinedAt = a.JoinedAt
        }).ToList() ?? new List<CollaborationAgentVo>();
        
        var tasks = await _collaborationService.GetTasksAsync(id);
        vo.Tasks = tasks?.Select(t => new CollaborationTaskVo
        {
            Id = t.Id,
            CollaborationId = t.CollaborationId,
            Title = t.Title,
            Description = t.Description,
            Prompt = t.Prompt,
            Status = t.Status,
            AssignedTo = t.AssignedTo,
            GitUrl = t.GitUrl,
            GitBranch = t.GitBranch,
            GitToken = t.GitCredentials,
            Config = t.Config,
            TaskFlow = t.TaskFlow,
            CompletedAt = t.CompletedAt,
            CreatedAt = t.CreatedAt
        }).ToList() ?? new List<CollaborationTaskVo>();

        return Ok(vo);
    }

    [HttpPost]
    public async Task<ActionResult<Core.Entities.Collaboration>> CreateCollaboration([FromBody] CreateCollaborationRequest request)
    {
        var userId = User.GetUserId();
        
        var collaboration = await _collaborationService.CreateAsync(
            request.Name,
            request.Description,
            request.Path,
            request.GitRepositoryUrl,
            request.GitBranch,
            request.GitUsername,
            request.GitEmail,
            request.GitAccessToken,
            request.Config,
            userId
        );
        
        await _logService.LogAsync(userId, "创建", "协作项目", $"创建协作项目: {request.Name}", null);
        
        return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaboration);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCollaboration(long id)
    {
        var userId = User.GetUserId();
        
        var result = await _collaborationService.DeleteAsync(id, userId);
        if (!result)
        {
            return NotFound();
        }
        
        await _logService.LogAsync(userId, "删除", "协作项目", $"删除协作项目: {id}", null);
        
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Core.Entities.Collaboration>> UpdateCollaboration(long id, [FromBody] CreateCollaborationRequest request)
    {
        var userId = User.GetUserId();
        
        var collaboration = await _collaborationService.GetByIdAsync(id, userId);
        if (collaboration == null)
        {
            return NotFound(new { success = false, message = "协作项目不存在" });
        }
        
        collaboration.Name = request.Name;
        collaboration.Description = request.Description;
        collaboration.Path = request.Path;
        collaboration.GitRepositoryUrl = request.GitRepositoryUrl;
        collaboration.GitBranch = request.GitBranch;
        collaboration.GitUsername = request.GitUsername;
        collaboration.GitEmail = request.GitEmail;
        collaboration.GitAccessToken = request.GitAccessToken;
        collaboration.Config = request.Config;
        
        var updatedCollaboration = await _collaborationService.UpdateAsync(collaboration);
        
        await _logService.LogAsync(userId, "更新", "协作项目", $"更新协作项目: {request.Name}", null);
        
        return Ok(updatedCollaboration);
    }

    [HttpPost("{id}/agents")]
    public async Task<ActionResult> AddAgentToCollaboration(long id, [FromBody] AddAgentRequest request)
    {
        var userId = User.GetUserId();
        
        try
        {
            var result = await _collaborationService.AddAgentAsync(id, request.AgentId, request.Role, request.CustomPrompt, userId);
            if (!result)
            {
                return BadRequest(new { success = false, message = "添加Agent失败，请检查协作和Agent是否存在" });
            }
            
            await _logService.LogAsync(userId, "添加", "协作Agent", $"向协作 {id} 添加Agent {request.AgentId}", null);
            
            return Ok(new { success = true, message = "Agent添加成功" });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
        {
            return BadRequest(new { success = false, message = "该Agent已经存在于协作中，请勿重复添加" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"添加Agent失败: {ex.Message}" });
        }
    }

    [HttpDelete("{id}/agents/{agentId}")]
    public async Task<ActionResult> RemoveAgentFromCollaboration(long id, long agentId)
    {
        var userId = User.GetUserId();
        
        var result = await _collaborationService.RemoveAgentAsync(id, agentId, userId);
        if (!result)
        {
            return NotFound();
        }
        
        return NoContent();
    }

    [HttpPatch("{id}/agents/{agentId}/role")]
    public async Task<ActionResult> UpdateAgentRole(long id, long agentId, [FromBody] UpdateAgentRoleRequest request)
    {
        var userId = User.GetUserId();
        
        try
        {
            var result = await _collaborationService.UpdateAgentRoleAsync(id, agentId, request.Role, request.CustomPrompt, userId);
            if (!result)
            {
                return NotFound(new { success = false, message = "协作或Agent不存在" });
            }
            
            await _logService.LogAsync(userId, "更新", "协作Agent角色", $"更新协作 {id} 中Agent {agentId} 的角色为 {request.Role}", null);
            
            return Ok(new { success = true, message = "角色更新成功" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"更新角色失败: {ex.Message}" });
        }
    }

    [HttpPost("{id}/tasks")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> CreateTask(long id, [FromBody] CreateTaskRequest request)
    {
        var userId = User.GetUserId();
        
        var task = await _collaborationService.CreateTaskAsync(
            id, 
            request.Title, 
            request.Description, 
            userId,
            request.Prompt,
            request.GitUrl,
            request.GitBranch,
            request.GitToken,
            request.AgentIds,
            request.Config,
            request.TaskFlow);
        
        return CreatedAtAction(nameof(GetCollaboration), new { id }, task);
    }

    [HttpPost("batch-delete-tasks")]
    public async Task<ActionResult> BatchDeleteTasks([FromBody] BatchDeleteTasksRequest request)
    {
        var userId = User.GetUserId();
        var successCount = 0;
        var failedCount = 0;
        
        foreach (var taskId in request.TaskIds)
        {
            var result = await _collaborationService.DeleteTaskAsync(taskId, userId);
            if (result)
            {
                successCount++;
            }
            else
            {
                failedCount++;
            }
        }
        
        return Ok(new { 
            success = true, 
            message = $"成功删除 {successCount} 个任务，失败 {failedCount} 个",
            successCount,
            failedCount
        });
    }

    [HttpPut("tasks/{taskId}")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> UpdateTask(long taskId, [FromBody] UpdateTaskRequest request)
    {
        var task = await _collaborationService.UpdateTaskAsync(
            taskId,
            request.Title,
            request.Description,
            request.Prompt,
            request.GitUrl,
            request.GitBranch,
            request.GitToken,
            request.AgentIds,
            request.Config,
            request.TaskFlow);
        
        return Ok(task);
    }

    [HttpDelete("tasks/{taskId}")]
    public async Task<ActionResult> DeleteTask(long taskId)
    {
        var userId = User.GetUserId();
        var result = await _collaborationService.DeleteTaskAsync(taskId, userId);
        
        if (!result)
        {
            return NotFound(new { message = "任务不存在或无权删除" });
        }
        
        return NoContent();
    }

    [HttpPatch("tasks/{taskId}/status")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> UpdateTaskStatus(long taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var userId = User.GetUserId();
        
        if (!Enum.TryParse<CollaborationTaskStatus>(request.Status, out var status))
        {
            return BadRequest("Invalid status value");
        }
        
        var task = await _collaborationService.UpdateTaskStatusAsync(taskId, status, userId);
        
        return Ok(task);
    }

    [HttpPatch("tasks/{taskId}/task-flow")]
    public async Task<ActionResult<Core.Entities.CollaborationTask>> UpdateTaskFlow(long taskId, [FromBody] UpdateTaskFlowRequest request)
    {
        var task = await _collaborationService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { message = "任务不存在" });
        }

        task.TaskFlow = request.TaskFlow;
        var updatedTask = await _collaborationTaskRepository.UpdateAsync(task);

        return Ok(updatedTask);
    }

    [HttpPost("tasks/{taskId}/execute")]
    public async Task<ActionResult> ExecuteTask(long taskId)
    {
        var userId = User.GetUserId();
        
        try
        {
            var task = await _collaborationService.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                return NotFound(new { success = false, message = "任务不存在" });
            }

            await _collaborationService.UpdateTaskStatusAsync(taskId, CollaborationTaskStatus.InProgress, userId);
            
            await _logService.LogAsync(userId, "执行", "协作任务", $"开始执行任务: {task.Title}", null);
            
            return Ok(new { success = true, message = "任务已启动，请查看工作流执行日志" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"启动任务失败: {ex.Message}" });
        }
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<List<Core.Entities.AgentMessage>>> GetCollaborationMessages(long id)
    {
        var collaboration = await _collaborationService.GetByIdAsync(id);
        if (collaboration == null)
        {
            return NotFound(new { error = "团队不存在" });
        }

        var messages = await _groupMessageRepository.GetByCollaborationIdAsync(collaboration.Id);
        
        return Ok(messages);
    }
    
    [HttpPost("test-email")]
    public async Task<ActionResult> TestEmailConfiguration([FromBody] TestEmailRequest request)
    {
        var userId = User.GetUserId();
        
        try
        {
            if (request.Smtp == null)
            {
                return BadRequest(new { success = false, message = "SMTP配置不能为空" });
            }

            var config = new SmtpTestConfig
            {
                Server = request.Smtp.Server,
                Port = request.Smtp.Port,
                Username = request.Smtp.Username,
                Password = request.Smtp.Password,
                FromEmail = request.Smtp.FromEmail,
                EnableSsl = request.Smtp.EnableSsl
            };

            var result = await _emailService.TestSmtpAsync(config);
            
            if (result.Success)
            {
                await _logService.LogAsync(userId, "测试", "SMTP配置", "测试邮件发送成功", null);
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试邮件发送失败");
            return BadRequest(new { success = false, message = $"测试邮件发送失败: {ex.Message}" });
        }
    }
    
    [HttpPost("{id}/chat")]
    public async Task<ActionResult> Chat(long id, [FromBody] CollaborationChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var members = await _collaborationAgentRepository.GetByCollaborationIdAsync(id);
            if (members.Count == 0)
                return BadRequest(new { success = false, message = "该协作没有Agent" });

            bool isPrivate = string.Equals(request.MessageType, "private", StringComparison.OrdinalIgnoreCase);
            string messageType = isPrivate ? "private" : "chat";

            _logger.LogInformation("协作聊天请求: CollaborationId={Id}, MessageType={MessageType}, Content={Content}, ToAgentId={ToAgentId}, MentionedAgentIds=[{MentionedIds}]", 
                id, messageType, request.Content, request.ToAgentId, string.Join(",", request.MentionedAgentIds ?? new List<string>()));

            List<long> targetAgentIds;
            bool isMentioned;

            if (isPrivate && request.ToAgentId.HasValue)
            {
                var targetId = request.ToAgentId.Value;
                if (!members.Any(m => m.AgentId == targetId))
                    return BadRequest(new { success = false, message = "目标Agent不在该协作中" });
                targetAgentIds = [targetId];
                isMentioned = true;
            }
            else
            {
                var mentionedIds = request.MentionedAgentIds ?? new List<string>();
                isMentioned = mentionedIds.Count > 0;

                if (isMentioned)
                {
                    targetAgentIds = mentionedIds
                        .Where(mid => long.TryParse(mid, out _))
                        .Select(long.Parse)
                        .Where(aid => members.Any(m => m.AgentId == aid))
                        .ToList();

                    if (targetAgentIds.Count == 0)
                        return BadRequest(new { success = false, message = "没有有效的被提及Agent" });
                }
                else
                {
                    var manager = members.FirstOrDefault(m =>
                        m.Role?.Equals("Manager", StringComparison.OrdinalIgnoreCase) == true);
                    targetAgentIds = [manager?.AgentId ?? members.First().AgentId];
                }
            }

            await _groupMessageRepository.CreateAsync(new GroupMessage
            {
                CollaborationId = id,
                MessageType = messageType,
                SenderType = "User",
                ToAgentId = isPrivate ? request.ToAgentId : null,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            });

            var recentMessages = isPrivate
                ? (await _groupMessageRepository.GetByCollaborationIdAsync(id, 20))
                    .Where(m => m.MessageType == "private" &&
                        (m.ToAgentId == request.ToAgentId || (m.FromAgentId == request.ToAgentId && m.SenderType == "Agent")))
                    .Take(10).ToList()
                : await _groupMessageRepository.GetByCollaborationIdAsync(id, 10);

            var results = new List<object>();

            _logger.LogInformation("协作聊天: CollaborationId={Id}, MessageType={MessageType}, 目标Agent数量={Count}, AgentIds=[{AgentIds}]", id, messageType, targetAgentIds.Count, string.Join(",", targetAgentIds));

            var agentTasks = targetAgentIds.Select(targetAgentId => ProcessAgentAsync(
                targetAgentId, id, request.Content, isMentioned, recentMessages, members, messageType, isPrivate ? request.ToAgentId : null, cancellationToken)).ToList();

            var agentResults = await Task.WhenAll(agentTasks);

            foreach (var result in agentResults)
            {
                if (result != null)
                {
                    results.Add(result);
                }
            }

            _logger.LogInformation("协作聊天完成: CollaborationId={Id}, 结果数量={ResultCount}", id, results.Count);
            return Ok(new { success = true, data = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "协作聊天失败: CollaborationId={Id}", id);
            return BadRequest(new { success = false, message = $"聊天失败: {ex.Message}" });
        }
    }

    private async Task<object?> ProcessAgentAsync(
        long targetAgentId,
        long collaborationId,
        string content,
        bool isMentioned,
        List<GroupMessage> recentMessages,
        List<CollaborationAgent> members,
        string messageType,
        long? toAgentId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理Agent: AgentId={AgentId}", targetAgentId);

        var agentEntity = await _agentRepository.GetByIdAsync(targetAgentId);
        if (agentEntity == null)
        {
            _logger.LogWarning("Agent不存在，跳过: AgentId={AgentId}", targetAgentId);
            return null;
        }

        _logger.LogInformation("Agent信息: AgentId={AgentId}, Name={Name}, LlmConfigs={LlmConfigs}", targetAgentId, agentEntity.Name, agentEntity.LlmConfigs);

        IChatClient chatClient;
        try
        {
            chatClient = await _agentFactoryService.CreateAgentAsync(targetAgentId);
            _logger.LogInformation("创建Agent客户端成功: AgentId={AgentId}", targetAgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建Agent客户端失败: AgentId={AgentId}, 错误={Error}", targetAgentId, ex.Message);
            var failContent = $"[系统提示] Agent {agentEntity.Name} 创建失败: {ex.Message}";
            await _groupMessageRepository.CreateAsync(new GroupMessage
            {
                CollaborationId = collaborationId,
                MessageType = messageType,
                SenderType = "Agent",
                FromAgentId = targetAgentId,
                ToAgentId = toAgentId,
                FromAgentName = agentEntity.Name,
                FromAgentRole = members.FirstOrDefault(m => m.AgentId == targetAgentId)?.Role ?? "Worker",
                FromAgentType = agentEntity.TypeName,
                FromAgentAvatar = agentEntity.Avatar,
                Content = failContent,
                IsMentioned = isMentioned,
                CreatedAt = DateTime.UtcNow
            });
            return new
            {
                fromAgentId = targetAgentId,
                fromAgentName = agentEntity.Name,
                fromAgentRole = members.FirstOrDefault(m => m.AgentId == targetAgentId)?.Role ?? "Worker",
                fromAgentType = agentEntity.TypeName ?? "",
                fromAgentAvatar = agentEntity.Avatar,
                modelName = (string?)null,
                llmConfigName = (string?)null,
                content = failContent,
                timestamp = DateTime.UtcNow,
                isMentioned
            };
        }

        var chatHistory = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(agentEntity.SystemPrompt))
            chatHistory.Add(new ChatMessage(ChatRole.System, agentEntity.SystemPrompt));

        try
        {
            var skillDefinitions = await _skillLoader.LoadSkillsForAgentAsync(targetAgentId);
            var skillInstructions = _skillLoader.BuildSkillInstructions(skillDefinitions);
            if (!string.IsNullOrEmpty(skillInstructions))
            {
                chatHistory.Add(new ChatMessage(ChatRole.System, skillInstructions));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载Agent技能指令失败: AgentId={AgentId}", targetAgentId);
        }

        foreach (var msg in recentMessages)
        {
            if (msg.SenderType == "User")
            {
                chatHistory.Add(new ChatMessage(ChatRole.User, msg.Content));
            }
            else if (msg.SenderType == "Agent")
            {
                if (msg.FromAgentId == targetAgentId)
                {
                    chatHistory.Add(new ChatMessage(ChatRole.Assistant, msg.Content));
                }
                else
                {
                    chatHistory.Add(new ChatMessage(ChatRole.User, $"[{msg.FromAgentName ?? "其他Agent"}]: {msg.Content}"));
                }
            }
        }

        var userPrompt = isMentioned
            ? content
            : $"[群聊消息] {content}";

        chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

        var memberInfo = members.FirstOrDefault(m => m.AgentId == targetAgentId);

        ChatResponse response;
        try
        {
            response = await chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);
            _logger.LogInformation("Agent调用成功: AgentId={AgentId}, ModelId={ModelId}, 回复长度={Length}", targetAgentId, response.ModelId, response.Messages.LastOrDefault()?.Text?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent调用失败: AgentId={AgentId}, 错误={Error}", targetAgentId, ex.Message);
            var failContent = $"[系统提示] Agent {agentEntity.Name} 调用失败: {ex.Message}";
            await _groupMessageRepository.CreateAsync(new GroupMessage
            {
                CollaborationId = collaborationId,
                MessageType = messageType,
                SenderType = "Agent",
                FromAgentId = targetAgentId,
                ToAgentId = toAgentId,
                FromAgentName = agentEntity.Name,
                FromAgentRole = memberInfo?.Role ?? "Worker",
                FromAgentType = agentEntity.TypeName,
                FromAgentAvatar = agentEntity.Avatar,
                Content = failContent,
                IsMentioned = isMentioned,
                CreatedAt = DateTime.UtcNow
            });
            return new
            {
                fromAgentId = targetAgentId,
                fromAgentName = agentEntity.Name,
                fromAgentRole = memberInfo?.Role ?? "Worker",
                fromAgentType = agentEntity.TypeName ?? "",
                fromAgentAvatar = agentEntity.Avatar,
                modelName = (string?)null,
                llmConfigName = (string?)null,
                content = failContent,
                timestamp = DateTime.UtcNow,
                isMentioned
            };
        }

        var reply = response.Messages.LastOrDefault()?.Text ?? "";

        string? modelName = response.ModelId;
        string? llmConfigName = null;
        if (!string.IsNullOrEmpty(agentEntity.LlmConfigs))
        {
            try
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var configs = System.Text.Json.JsonSerializer.Deserialize<List<LlmConfigInfoVo>>(agentEntity.LlmConfigs, jsonOptions);

                var primaryConfig = configs?.FirstOrDefault(c => c.IsPrimary) ?? configs?.FirstOrDefault();
                modelName ??= primaryConfig?.ModelName;
                llmConfigName = primaryConfig?.LlmConfigName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析LlmConfigs失败: AgentId={AgentId}", targetAgentId);
            }
        }

        _logger.LogInformation("最终模型信息: AgentId={AgentId}, ModelName={ModelName}, LlmConfigName={LlmConfigName}", targetAgentId, modelName, llmConfigName);

        await _groupMessageRepository.CreateAsync(new GroupMessage
        {
            CollaborationId = collaborationId,
            MessageType = messageType,
            SenderType = "Agent",
            FromAgentId = targetAgentId,
            ToAgentId = toAgentId,
            FromAgentName = agentEntity.Name,
            FromAgentRole = memberInfo?.Role ?? "Worker",
            FromAgentType = agentEntity.TypeName,
            FromAgentAvatar = agentEntity.Avatar,
            ModelName = modelName,
            LlmConfigName = llmConfigName,
            Content = reply,
            IsMentioned = isMentioned,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Agent {AgentId}({AgentName}) 回复成功", targetAgentId, agentEntity.Name);

        return new
        {
            fromAgentId = targetAgentId,
            fromAgentName = agentEntity.Name,
            fromAgentRole = memberInfo?.Role ?? "Worker",
            fromAgentType = agentEntity.TypeName ?? "",
            fromAgentAvatar = agentEntity.Avatar,
            modelName,
            llmConfigName,
            content = reply,
            timestamp = DateTime.UtcNow,
            isMentioned
        };
    }

    [HttpGet("{id}/chat/history")]
    public async Task<ActionResult> GetChatHistory(long id, [FromQuery] int limit = 20, [FromQuery] long? beforeId = null, [FromQuery] string? messageType = null, [FromQuery] long? toAgentId = null)
    {
        try
        {
            var allMessages = await _groupMessageRepository.GetByCollaborationIdAsync(id, limit * 3, beforeId);

            var filtered = allMessages.AsEnumerable();
            if (!string.IsNullOrEmpty(messageType) && messageType.Equals("private", StringComparison.OrdinalIgnoreCase) && toAgentId.HasValue)
            {
                filtered = filtered.Where(m =>
                    m.MessageType == "private" &&
                    (m.ToAgentId == toAgentId.Value || (m.FromAgentId == toAgentId.Value && m.SenderType == "Agent")));
            }
            else if (!string.IsNullOrEmpty(messageType))
            {
                filtered = filtered.Where(m => m.MessageType == messageType);
            }

            var messages = filtered.Take(limit + 1).ToList();
            var hasMore = messages.Count > limit;
            if (hasMore) messages = messages.Take(limit).ToList();

            var result = messages.Select(m => new
            {
                id = m.Id,
                fromAgentId = m.FromAgentId,
                fromAgentName = m.FromAgentName ?? "我",
                fromAgentRole = m.FromAgentRole,
                fromAgentType = m.FromAgentType,
                fromAgentAvatar = m.FromAgentAvatar,
                toAgentId = m.ToAgentId,
                messageType = m.MessageType,
                modelName = m.ModelName,
                llmConfigName = m.LlmConfigName,
                senderType = m.SenderType,
                content = m.Content,
                isMentioned = m.IsMentioned,
                timestamp = m.CreatedAt
            }).ToList();

            return Ok(new { success = true, data = result, hasMore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天历史失败: CollaborationId={Id}", id);
            return BadRequest(new { success = false, message = $"获取聊天历史失败: {ex.Message}" });
        }
    }

    [HttpPost("{id}/test-email")]
    public async Task<ActionResult> TestEmailConfigurationFromDb(long id)
    {
        var userId = User.GetUserId();
        
        try
        {
            var result = await _emailService.TestSmtpFromCollaborationAsync(id, userId);
            
            if (result.Success)
            {
                await _logService.LogAsync(userId, "测试", "SMTP配置", $"测试邮件发送成功: 协作{id}", null);
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"测试邮件发送失败: {ex.Message}" });
        }
    }
}

public class TestEmailRequest
{
    public SmtpTestConfig? Smtp { get; set; }
}

public class CollaborationChatRequest
{
    public string Content { get; set; } = string.Empty;
    public List<string>? MentionedAgentIds { get; set; }
    public string? MessageType { get; set; }
    public long? ToAgentId { get; set; }
}

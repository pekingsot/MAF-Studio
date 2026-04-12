using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.VOs;
using MAFStudio.Application.Mappers;
using MAFStudio.Application.DTOs.Requests;
using System.Security.Claims;
using MAFStudio.Core.Enums;
using MAFStudio.Api.Extensions;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollaborationsController : ControllerBase
{
    private readonly ICollaborationService _collaborationService;
    private readonly IAuthService _authService;
    private readonly IOperationLogService _logService;
    private readonly IAgentMessageRepository _agentMessageRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<CollaborationsController> _logger;

    public CollaborationsController(
        ICollaborationService collaborationService, 
        IAuthService authService, 
        IOperationLogService logService,
        IAgentMessageRepository agentMessageRepository,
        IEmailService emailService,
        ILogger<CollaborationsController> logger)
    {
        _collaborationService = collaborationService;
        _authService = authService;
        _logService = logService;
        _agentMessageRepository = agentMessageRepository;
        _emailService = emailService;
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
            request.Config);
        
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
            request.Config);
        
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

        var messages = await _agentMessageRepository.GetByCollaborationIdAsync(collaboration.Id);
        
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

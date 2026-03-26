using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IAgentService _agentService;
        private readonly IAuthService _authService;

        public MessagesController(IMessageService messageService, IAgentService agentService, IAuthService authService)
        {
            _messageService = messageService;
            _agentService = agentService;
            _authService = authService;
        }

        [HttpGet("agent/{agentId}")]
        public async Task<ActionResult<List<AgentMessage>>> GetMessagesForAgent(
            Guid agentId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? before = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent = await _agentService.GetAgentByIdAsync(agentId);
            if (agent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && agent.UserId != userId)
            {
                return Forbid();
            }
            
            var messages = await _messageService.GetMessagesForAgentAsync(agentId, page, pageSize, before);
            return Ok(messages);
        }

        [HttpGet("conversation/{agent1Id}/{agent2Id}")]
        public async Task<ActionResult<List<AgentMessage>>> GetConversation(
            Guid agent1Id, 
            Guid agent2Id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? before = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var agent1 = await _agentService.GetAgentByIdAsync(agent1Id);
            var agent2 = await _agentService.GetAgentByIdAsync(agent2Id);
            
            if (agent1 == null || agent2 == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && agent1.UserId != userId && agent2.UserId != userId)
            {
                return Forbid();
            }
            
            var messages = await _messageService.GetConversationAsync(agent1Id, agent2Id, page, pageSize, before);
            return Ok(messages);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<AgentMessage>>> GetAllHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] DateTime? before = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var messages = await _messageService.GetHistoryMessagesAsync(userId!, isAdmin, page, pageSize, before);
            return Ok(messages);
        }

        /// <summary>
        /// 获取协作项目的消息历史
        /// </summary>
        [HttpGet("collaboration/{collaborationId}")]
        public async Task<ActionResult<object>> GetCollaborationMessages(
            Guid collaborationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? before = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var messages = await _messageService.GetCollaborationMessagesAsync(collaborationId, page, pageSize, before);
            var total = await _messageService.GetCollaborationMessagesCountAsync(collaborationId);
            
            var formattedMessages = messages.Select(m => new
            {
                m.Id,
                m.FromAgentId,
                FromAgentName = m.SenderType == SenderType.User ? (m.SenderName ?? "用户") : (m.FromAgent?.Name ?? "智能体"),
                FromAgentAvatar = m.FromAgent?.Avatar,
                m.ToAgentId,
                ToAgentName = m.ToAgent?.Name,
                ToAgentAvatar = m.ToAgent?.Avatar,
                m.Content,
                Type = m.Type.ToString().ToLower(),
                m.CreatedAt,
                SenderType = m.SenderType.ToString(),
                m.SenderName
            });
            
            return Ok(new { messages = formattedMessages, total, page, pageSize });
        }

        [HttpPost]
        public async Task<ActionResult<AgentMessage>> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = await _authService.IsAdminAsync(userId!);
            
            var fromAgent = await _agentService.GetAgentByIdAsync(request.FromAgentId);
            var toAgent = await _agentService.GetAgentByIdAsync(request.ToAgentId);
            
            if (fromAgent == null || toAgent == null)
            {
                return NotFound();
            }
            
            if (!isAdmin && fromAgent.UserId != userId)
            {
                return Forbid();
            }
            
            var messageType = Enum.Parse<MessageType>(request.Type);
            var message = await _messageService.SendMessageAsync(
                request.FromAgentId,
                request.ToAgentId,
                request.Content,
                messageType
            );
            return CreatedAtAction(nameof(GetMessagesForAgent), new { agentId = request.ToAgentId }, message);
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<AgentMessage>> UpdateMessageStatus(Guid id, [FromBody] UpdateMessageStatusRequest request)
        {
            var message = await _messageService.UpdateMessageStatusAsync(id, request.Status);
            if (message == null)
            {
                return NotFound();
            }
            return Ok(message);
        }
    }

    public class SendMessageRequest
    {
        public Guid FromAgentId { get; set; }
        public Guid ToAgentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "Text";
    }

    public class UpdateMessageStatusRequest
    {
        public MessageStatus Status { get; set; }
    }
}
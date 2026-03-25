using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 消息服务实现
    /// 提供智能体消息的发送、查询和管理功能
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AgentMessage>> GetMessagesForAgentAsync(Guid agentId, int page = 1, int pageSize = 50, DateTime? before = null)
        {
            var query = _context.AgentMessages
                .AsNoTracking()
                .Where(m => m.ToAgentId == agentId || m.FromAgentId == agentId)
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadAgentsAsync(messages);
            return messages;
        }

        public async Task<AgentMessage> SendMessageAsync(Guid fromAgentId, Guid toAgentId, string content, MessageType type)
        {
            var message = new AgentMessage
            {
                Id = Guid.NewGuid(),
                FromAgentId = fromAgentId,
                ToAgentId = toAgentId,
                Content = content,
                Type = type,
                Status = MessageStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.AgentMessages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<AgentMessage?> UpdateMessageStatusAsync(Guid messageId, MessageStatus status)
        {
            var message = await _context.AgentMessages.FindAsync(messageId);
            if (message == null) return null;

            message.Status = status;
            
            if (status == MessageStatus.Completed || status == MessageStatus.Failed)
            {
                message.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<List<AgentMessage>> GetConversationAsync(Guid agent1Id, Guid agent2Id, int page = 1, int pageSize = 50, DateTime? before = null)
        {
            var query = _context.AgentMessages
                .AsNoTracking()
                .Where(m => 
                    (m.FromAgentId == agent1Id && m.ToAgentId == agent2Id) ||
                    (m.FromAgentId == agent2Id && m.ToAgentId == agent1Id))
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadAgentsAsync(messages);
            return messages;
        }

        public async Task<List<AgentMessage>> GetHistoryMessagesAsync(string userId, bool isAdmin, int page = 1, int pageSize = 50, DateTime? before = null)
        {
            List<Guid> userAgentIds = new List<Guid>();
            
            if (!isAdmin)
            {
                userAgentIds = await _context.Agents
                    .AsNoTracking()
                    .Where(a => a.UserId == userId)
                    .Select(a => a.Id)
                    .ToListAsync();
            }

            var query = _context.AgentMessages
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(m => 
                    userAgentIds.Contains(m.FromAgentId) || 
                    userAgentIds.Contains(m.ToAgentId));
            }

            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadAgentsAsync(messages);
            return messages;
        }

        public async Task<List<AgentMessage>> GetCollaborationMessagesAsync(Guid collaborationId, int page = 1, int pageSize = 20, DateTime? before = null)
        {
            var query = _context.AgentMessages
                .AsNoTracking()
                .Where(m => m.CollaborationId == collaborationId)
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadAgentsAsync(messages);
            return messages;
        }

        public async Task<int> GetCollaborationMessagesCountAsync(Guid collaborationId)
        {
            return await _context.AgentMessages
                .CountAsync(m => m.CollaborationId == collaborationId);
        }

        private async Task LoadAgentsAsync(List<AgentMessage> messages)
        {
            if (messages.Count == 0) return;

            var fromAgentIds = messages.Select(m => m.FromAgentId).Distinct().ToList();
            var toAgentIds = messages.Select(m => m.ToAgentId).Distinct().ToList();
            var allAgentIds = fromAgentIds.Union(toAgentIds).Distinct().ToList();

            var agents = await _context.Agents
                .AsNoTracking()
                .Where(a => allAgentIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);

            foreach (var message in messages)
            {
                if (agents.TryGetValue(message.FromAgentId, out var fromAgent))
                {
                    message.FromAgent = fromAgent;
                }
                if (agents.TryGetValue(message.ToAgentId, out var toAgent))
                {
                    message.ToAgent = toAgent;
                }
            }
        }
    }
}

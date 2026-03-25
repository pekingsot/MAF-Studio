using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 智能体服务实现
    /// 提供智能体的增删改查操作
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly ApplicationDbContext _context;

        public AgentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Agent>> GetAllAgentsAsync()
        {
            var agents = await _context.Agents
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .ToListAsync();

            await LoadLLMConfigsAsync(agents);
            return agents;
        }

        public async Task<List<Agent>> GetAgentsByUserIdAsync(string userId, bool isAdmin)
        {
            var query = _context.Agents
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(a => a.UserId == userId);
            }

            var agents = await query.OrderBy(a => a.Name).ToListAsync();
            await LoadLLMConfigsAsync(agents);
            return agents;
        }

        public async Task<Agent?> GetAgentByIdAsync(Guid id)
        {
            var agent = await _context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agent != null && agent.LLMConfigId.HasValue)
            {
                agent.LLMConfig = await _context.LLMConfigs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == agent.LLMConfigId.Value);
            }

            return agent;
        }

        public async Task<Agent> CreateAgentAsync(string name, string description, string type, string configuration, string? avatar, string userId, Guid? llmConfigId = null)
        {
            var agent = new Agent
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Type = type,
                Configuration = configuration,
                Avatar = avatar ?? "🤖",
                UserId = userId,
                LLMConfigId = llmConfigId,
                Status = AgentStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            return agent;
        }

        public async Task<Agent?> UpdateAgentAsync(Guid id, string? name, string? description, string? configuration, string? avatar, Guid? llmConfigId = null)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null) return null;

            if (!string.IsNullOrEmpty(name))
                agent.Name = name;

            if (description != null)
                agent.Description = description;

            if (configuration != null)
                agent.Configuration = configuration;

            if (avatar != null)
                agent.Avatar = avatar;

            if (llmConfigId.HasValue)
                agent.LLMConfigId = llmConfigId;

            agent.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return agent;
        }

        public async Task<bool> DeleteAgentAsync(Guid id)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null) return false;

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Agent?> UpdateAgentStatusAsync(Guid id, AgentStatus status)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null) return null;

            agent.Status = status;
            agent.LastActiveAt = DateTime.UtcNow;
            agent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return agent;
        }

        private async Task LoadLLMConfigsAsync(List<Agent> agents)
        {
            var llmConfigIds = agents
                .Where(a => a.LLMConfigId.HasValue)
                .Select(a => a.LLMConfigId!.Value)
                .Distinct()
                .ToList();

            if (llmConfigIds.Count == 0) return;

            var llmConfigs = await _context.LLMConfigs
                .AsNoTracking()
                .Where(c => llmConfigIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            foreach (var agent in agents)
            {
                if (agent.LLMConfigId.HasValue && llmConfigs.TryGetValue(agent.LLMConfigId.Value, out var config))
                {
                    agent.LLMConfig = config;
                }
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 协作服务实现
    /// 提供协作管理、任务分配、智能体协作等功能
    /// </summary>
    public class CollaborationService : ICollaborationService
    {
        private readonly ApplicationDbContext _context;

        public CollaborationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Collaboration>> GetAllCollaborationsAsync(string? userId = null)
        {
            var query = _context.Collaborations
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }

            var collaborations = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            if (collaborations.Count == 0)
                return collaborations;

            var collaborationIds = collaborations.Select(c => c.Id).ToList();

            var agents = await _context.CollaborationAgents
                .AsNoTracking()
                .Where(ca => collaborationIds.Contains(ca.CollaborationId))
                .ToListAsync();

            var tasks = await _context.CollaborationTasks
                .AsNoTracking()
                .Where(ct => collaborationIds.Contains(ct.CollaborationId))
                .ToListAsync();

            var agentIds = agents.Select(a => a.AgentId).Distinct().ToList();
            var agentData = await _context.Agents
                .AsNoTracking()
                .Where(a => agentIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);

            foreach (var ca in agents)
            {
                if (agentData.TryGetValue(ca.AgentId, out var agent))
                {
                    ca.Agent = agent;
                }
            }

            foreach (var c in collaborations)
            {
                c.Agents = agents.Where(a => a.CollaborationId == c.Id).ToList();
                c.Tasks = tasks.Where(t => t.CollaborationId == c.Id).ToList();
            }

            return collaborations;
        }

        public async Task<List<Collaboration>> GetCollaborationsByUserIdAsync(string userId)
        {
            return await GetAllCollaborationsAsync(userId);
        }

        public async Task<Collaboration?> GetCollaborationByIdAsync(Guid id, string? userId = null)
        {
            var query = _context.Collaborations
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.UserId == userId);
            }

            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Collaboration> CreateCollaborationAsync(
            string name, 
            string? description, 
            string? path,
            string? gitRepositoryUrl = null,
            string? gitBranch = null,
            string? gitUsername = null,
            string? gitEmail = null,
            string? gitAccessToken = null,
            string? userId = null)
        {
            var collaboration = new Collaboration
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                Path = path,
                GitRepositoryUrl = gitRepositoryUrl,
                GitBranch = gitBranch ?? "main",
                GitUsername = gitUsername,
                GitEmail = gitEmail,
                GitAccessToken = gitAccessToken,
                Status = CollaborationStatus.Active,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Collaborations.Add(collaboration);
            await _context.SaveChangesAsync();

            return collaboration;
        }

        public async Task<Collaboration?> UpdateCollaborationAsync(
            Guid id, 
            string name, 
            string? description, 
            string? path,
            string? gitRepositoryUrl = null,
            string? gitBranch = null,
            string? gitUsername = null,
            string? gitEmail = null,
            string? gitAccessToken = null,
            string? userId = null)
        {
            var collaboration = await _context.Collaborations.FindAsync(id);
            if (collaboration == null) return null;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return null;

            collaboration.Name = name;
            collaboration.Description = description;
            collaboration.Path = path;
            collaboration.GitRepositoryUrl = gitRepositoryUrl;
            collaboration.GitBranch = gitBranch ?? collaboration.GitBranch;
            collaboration.GitUsername = gitUsername;
            collaboration.GitEmail = gitEmail;
            if (!string.IsNullOrEmpty(gitAccessToken))
            {
                collaboration.GitAccessToken = gitAccessToken;
            }
            collaboration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return collaboration;
        }

        public async Task<bool> DeleteCollaborationAsync(Guid id, string? userId = null)
        {
            var collaboration = await _context.Collaborations.FindAsync(id);
            if (collaboration == null) return false;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return false;

            var agents = await _context.CollaborationAgents
                .Where(ca => ca.CollaborationId == id)
                .ToListAsync();
            _context.CollaborationAgents.RemoveRange(agents);

            var tasks = await _context.CollaborationTasks
                .Where(ct => ct.CollaborationId == id)
                .ToListAsync();
            _context.CollaborationTasks.RemoveRange(tasks);

            _context.Collaborations.Remove(collaboration);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Collaboration?> AddAgentToCollaborationAsync(Guid collaborationId, Guid agentId, string? role, string? userId = null)
        {
            var collaboration = await _context.Collaborations.FindAsync(collaborationId);
            if (collaboration == null) return null;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return null;

            var agentExists = await _context.Agents.AnyAsync(a => a.Id == agentId);
            if (!agentExists) return null;

            var collaborationAgent = new CollaborationAgent
            {
                Id = Guid.NewGuid(),
                CollaborationId = collaborationId,
                AgentId = agentId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            _context.CollaborationAgents.Add(collaborationAgent);
            collaboration.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return collaboration;
        }

        public async Task<CollaborationTask?> CreateTaskAsync(Guid collaborationId, string title, string? description, string? userId = null)
        {
            var collaboration = await _context.Collaborations.FindAsync(collaborationId);
            if (collaboration == null) return null;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return null;

            var task = new CollaborationTask
            {
                Id = Guid.NewGuid(),
                CollaborationId = collaborationId,
                Title = title,
                Description = description,
                Status = Data.TaskStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.CollaborationTasks.Add(task);
            
            collaboration.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<CollaborationTask?> UpdateTaskStatusAsync(Guid taskId, Data.TaskStatus status, string? userId = null)
        {
            var task = await _context.CollaborationTasks.FindAsync(taskId);
            if (task == null) return null;

            var collaboration = await _context.Collaborations.FindAsync(task.CollaborationId);
            if (collaboration == null) return null;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return null;

            task.Status = status;
            
            if (status == Data.TaskStatus.Completed || status == Data.TaskStatus.Failed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<bool> DeleteTaskAsync(Guid taskId, string? userId = null)
        {
            var task = await _context.CollaborationTasks.FindAsync(taskId);
            if (task == null) return false;

            var collaboration = await _context.Collaborations.FindAsync(task.CollaborationId);
            if (collaboration == null) return false;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return false;

            _context.CollaborationTasks.Remove(task);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveAgentFromCollaborationAsync(Guid collaborationId, Guid agentId, string? userId = null)
        {
            var collaboration = await _context.Collaborations.FindAsync(collaborationId);
            if (collaboration == null) return false;
            
            if (!string.IsNullOrEmpty(userId) && collaboration.UserId != userId) return false;
            
            var collaborationAgent = await _context.CollaborationAgents
                .FirstOrDefaultAsync(ca => ca.CollaborationId == collaborationId && ca.AgentId == agentId);
            
            if (collaborationAgent == null) return false;

            _context.CollaborationAgents.Remove(collaborationAgent);
            
            collaboration.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}

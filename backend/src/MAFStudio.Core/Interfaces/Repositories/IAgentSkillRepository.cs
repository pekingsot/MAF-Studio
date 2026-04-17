using MAFStudio.Core.Entities;

namespace MAFStudio.Core.Interfaces.Repositories;

public interface IAgentSkillRepository
{
    Task<AgentSkill?> GetByIdAsync(long id);
    Task<List<AgentSkill>> GetByAgentIdAsync(long agentId);
    Task<List<AgentSkill>> GetEnabledByAgentIdAsync(long agentId);
    Task<AgentSkill> CreateAsync(AgentSkill skill);
    Task<AgentSkill> UpdateAsync(AgentSkill skill);
    Task<bool> DeleteAsync(long id);
    Task<bool> DeleteByAgentAndNameAsync(long agentId, string skillName);
}

public interface ISkillTemplateRepository
{
    Task<SkillTemplate?> GetByIdAsync(long id);
    Task<List<SkillTemplate>> GetAllAsync();
    Task<List<SkillTemplate>> GetByCategoryAsync(string category);
    Task<SkillTemplate> CreateAsync(SkillTemplate template);
    Task<SkillTemplate> UpdateAsync(SkillTemplate template);
    Task<bool> DeleteAsync(long id);
    Task IncrementUsageCountAsync(long id);
}

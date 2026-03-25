using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 智能体类型服务实现
    /// 提供智能体类型的增删改查操作
    /// </summary>
    public class AgentTypeService : IAgentTypeService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AgentTypeService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有智能体类型
        /// </summary>
        public async Task<List<AgentType>> GetAllTypesAsync(string? userId = null)
        {
            return await _context.AgentTypes
                .AsNoTracking()
                .Where(t => t.IsSystem || t.UserId == userId)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取启用的智能体类型
        /// </summary>
        public async Task<List<AgentType>> GetEnabledTypesAsync(string? userId = null)
        {
            return await _context.AgentTypes
                .AsNoTracking()
                .Where(t => t.IsEnabled && (t.IsSystem || t.UserId == userId))
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取类型
        /// </summary>
        public async Task<AgentType?> GetByIdAsync(Guid id)
        {
            return await _context.AgentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// 根据编码获取类型
        /// </summary>
        public async Task<AgentType?> GetByCodeAsync(string code)
        {
            return await _context.AgentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == code);
        }

        /// <summary>
        /// 创建智能体类型
        /// </summary>
        public async Task<AgentType> CreateTypeAsync(AgentType type, string? userId = null)
        {
            type.Id = Guid.NewGuid();
            type.CreatedAt = DateTime.UtcNow;
            type.UserId = userId;
            type.IsSystem = false;

            _context.AgentTypes.Add(type);
            await _context.SaveChangesAsync();

            return type;
        }

        /// <summary>
        /// 更新智能体类型
        /// </summary>
        public async Task<AgentType?> UpdateTypeAsync(Guid id, AgentType type, string? userId = null)
        {
            var existing = await _context.AgentTypes.FindAsync(id);
            if (existing == null) return null;
            
            // 系统类型只能由管理员修改，用户只能修改自己的类型
            if (existing.IsSystem && userId != null) return null;
            if (!existing.IsSystem && existing.UserId != userId) return null;

            existing.Name = type.Name;
            existing.Description = type.Description;
            existing.DefaultSystemPrompt = type.DefaultSystemPrompt;
            existing.DefaultTemperature = type.DefaultTemperature;
            existing.DefaultMaxTokens = type.DefaultMaxTokens;
            existing.Icon = type.Icon;
            existing.SortOrder = type.SortOrder;
            existing.IsEnabled = type.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// 删除智能体类型
        /// </summary>
        public async Task<bool> DeleteTypeAsync(Guid id, string? userId = null)
        {
            var type = await _context.AgentTypes.FindAsync(id);
            if (type == null || type.IsSystem) return false;
            
            // 只能删除自己创建的类型
            if (type.UserId != userId) return false;

            _context.AgentTypes.Remove(type);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// 启用/禁用智能体类型
        /// </summary>
        public async Task<AgentType?> ToggleEnableAsync(Guid id, bool isEnabled, string? userId = null)
        {
            var type = await _context.AgentTypes.FindAsync(id);
            if (type == null) return null;
            
            // 系统类型只能由管理员禁用，用户只能禁用自己的类型
            if (type.IsSystem && userId != null) return null;
            if (!type.IsSystem && type.UserId != userId) return null;

            type.IsEnabled = isEnabled;
            type.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return type;
        }

        /// <summary>
        /// 初始化默认类型
        /// </summary>
        public async Task InitializeDefaultTypesAsync()
        {
            // 先删除现有数据
            var existingTypes = await _context.AgentTypes.ToListAsync();
            if (existingTypes.Any())
            {
                _context.AgentTypes.RemoveRange(existingTypes);
                await _context.SaveChangesAsync();
            }

            var defaultTypes = new List<AgentType>
            {
                new AgentType
                {
                    Code = "product_manager",
                    Name = "产品经理",
                    Description = "负责产品规划、需求分析和用户体验设计",
                    DefaultSystemPrompt = "你是一位资深产品经理，擅长产品规划、需求分析和用户体验设计。你能够深入理解用户需求，制定产品路线图，编写PRD文档，并协调各方资源推动产品落地。请用专业的产品思维回答问题，关注用户价值、商业价值和技术可行性。",
                    Icon = "📱",
                    SortOrder = 1,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "ui_designer",
                    Name = "UI设计师",
                    Description = "负责界面设计和视觉规范制定",
                    DefaultSystemPrompt = "你是一位专业的UI设计师，擅长界面设计、交互设计和视觉规范制定。你熟悉设计系统和组件库，能够输出高质量的设计稿和设计规范。请从用户体验和视觉美学的角度提供建议，关注设计的一致性和可用性。",
                    Icon = "🎨",
                    SortOrder = 2,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "frontend_dev",
                    Name = "前端开发工程师",
                    Description = "负责Web和移动端前端开发",
                    DefaultSystemPrompt = "你是一位资深前端开发工程师，精通React、Vue、TypeScript等主流前端技术栈。你熟悉前端工程化、性能优化、跨端开发，能够编写高质量、可维护的前端代码。请遵循最佳实践，关注代码质量、用户体验和性能表现。",
                    Icon = "💻",
                    SortOrder = 3,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "backend_dev",
                    Name = "后端开发工程师",
                    Description = "负责服务端架构和API开发",
                    DefaultSystemPrompt = "你是一位资深后端开发工程师，精通Java、Python、Go等后端语言，熟悉微服务架构、分布式系统、数据库设计。你能够设计高可用、高性能的后端系统，编写规范的API文档。请关注系统架构、代码质量、安全性和可扩展性。",
                    Icon = "⚙️",
                    SortOrder = 4,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "qa_engineer",
                    Name = "测试工程师",
                    Description = "负责软件测试和质量保证",
                    DefaultSystemPrompt = "你是一位专业的测试工程师，擅长测试用例设计、自动化测试、性能测试和安全测试。你熟悉各种测试框架和工具，能够制定完整的测试计划，发现并追踪缺陷。请从质量保证的角度提供测试建议，确保产品质量。",
                    Icon = "🔍",
                    SortOrder = 5,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "devops_engineer",
                    Name = "运维工程师",
                    Description = "负责系统运维和DevOps实践",
                    DefaultSystemPrompt = "你是一位资深运维工程师，精通Linux系统管理、容器化技术(Docker/K8s)、CI/CD流水线、监控告警和故障排查。你能够保障系统稳定运行，优化部署流程。请关注系统可靠性、自动化运维和成本优化。",
                    Icon = "🔧",
                    SortOrder = 6,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "architect",
                    Name = "架构师",
                    Description = "负责系统架构设计和技术选型",
                    DefaultSystemPrompt = "你是一位资深架构师，具备丰富的系统设计经验，擅长分布式系统、微服务架构、高并发高可用系统设计。你能够进行技术选型、架构评审和技术攻关。请从全局视角提供架构建议，平衡业务需求、技术可行性和成本。",
                    Icon = "🏗️",
                    SortOrder = 7,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "data_analyst",
                    Name = "数据分析师",
                    Description = "负责数据分析和商业洞察",
                    DefaultSystemPrompt = "你是一位专业的数据分析师，精通数据分析方法、SQL、Python数据分析库，熟悉数据可视化和BI工具。你能够从数据中发现业务洞察，支持数据驱动决策。请用数据说话，提供有价值的分析报告和建议。",
                    Icon = "📊",
                    SortOrder = 8,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "algorithm_engineer",
                    Name = "算法工程师",
                    Description = "负责算法研发和模型优化",
                    DefaultSystemPrompt = "你是一位资深算法工程师，精通机器学习、深度学习算法，熟悉TensorFlow、PyTorch等框架。你能够设计并优化算法模型，解决复杂的业务问题。请关注算法效果、性能和工程落地能力。",
                    Icon = "🧠",
                    SortOrder = 9,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "operations",
                    Name = "运营专员",
                    Description = "负责产品运营和用户增长",
                    DefaultSystemPrompt = "你是一位资深运营专员，擅长用户运营、活动运营、内容运营和数据分析。你熟悉各种运营工具和方法论，能够制定运营策略并执行落地。请关注用户增长、留存和转化，提供可落地的运营建议。",
                    Icon = "📈",
                    SortOrder = 10,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "content_editor",
                    Name = "内容编辑",
                    Description = "负责内容创作和编辑",
                    DefaultSystemPrompt = "你是一位专业的内容编辑，擅长内容策划、文案撰写和内容优化。你熟悉SEO优化和内容营销，能够创作高质量、有吸引力的内容。请关注内容质量、用户需求和传播效果，提供专业的内容建议。",
                    Icon = "✍️",
                    SortOrder = 11,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "security_engineer",
                    Name = "安全工程师",
                    Description = "负责系统安全和渗透测试",
                    DefaultSystemPrompt = "你是一位资深安全工程师，精通网络安全、应用安全、渗透测试和安全加固。你熟悉各种安全漏洞和攻击手段，能够进行安全评估和应急响应。请从安全角度提供专业建议，保障系统和数据安全。",
                    Icon = "🔒",
                    SortOrder = 12,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "tech_lead",
                    Name = "技术负责人",
                    Description = "负责技术团队管理和技术决策",
                    DefaultSystemPrompt = "你是一位经验丰富的技术负责人，具备技术管理和团队管理能力。你能够制定技术规划、进行技术决策、协调团队资源、推动项目落地。请从技术管理角度提供建议，关注技术发展、团队成长和业务价值。",
                    Icon = "👨‍💼",
                    SortOrder = 13,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "scrum_master",
                    Name = "项目经理",
                    Description = "负责项目管理和敏捷实践",
                    DefaultSystemPrompt = "你是一位专业的项目经理，精通敏捷开发方法论(Scrum/Kanban)，擅长项目规划、进度管理、风险控制和团队协调。你能够推动项目按时交付，解决项目中的各种问题。请关注项目目标、团队协作和持续改进。",
                    Icon = "📋",
                    SortOrder = 14,
                    IsSystem = true
                },
                new AgentType
                {
                    Code = "fullstack_dev",
                    Name = "全栈工程师",
                    Description = "负责前后端全栈开发",
                    DefaultSystemPrompt = "你是一位全栈工程师，精通前后端开发技术，能够独立完成从需求分析到部署上线的全流程开发。你具备系统思维，能够从全局角度解决问题。请关注技术广度和深度，提供端到端的技术解决方案。",
                    Icon = "🚀",
                    SortOrder = 15,
                    IsSystem = true
                }
            };

            _context.AgentTypes.AddRange(defaultTypes);
            await _context.SaveChangesAsync();
        }
    }
}

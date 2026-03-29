using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;

namespace MAFStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDapperContext, DapperContext>();
        
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentModelRepository, AgentModelRepository>();
        services.AddScoped<IAgentTypeRepository, AgentTypeRepository>();
        services.AddScoped<IAgentMessageRepository, AgentMessageRepository>();
        services.AddScoped<ICollaborationRepository, CollaborationRepository>();
        services.AddScoped<ICollaborationTaskRepository, CollaborationTaskRepository>();
        services.AddScoped<ILlmConfigRepository, LlmConfigRepository>();
        services.AddScoped<ILlmModelConfigRepository, LlmModelConfigRepository>();
        services.AddScoped<IOperationLogRepository, OperationLogRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        
        return services;
    }
}

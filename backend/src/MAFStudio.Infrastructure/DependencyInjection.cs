using Microsoft.Extensions.DependencyInjection;

namespace MAFStudio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<Data.IDapperContext, Data.DapperContext>();
        
        services.AddScoped<Core.Interfaces.Repositories.IUserRepository, Data.Repositories.UserRepository>();
        services.AddScoped<Core.Interfaces.Repositories.IAgentRepository, Data.Repositories.AgentRepository>();
        services.AddScoped<Core.Interfaces.Repositories.IAgentTypeRepository, Data.Repositories.AgentTypeRepository>();
        services.AddScoped<Core.Interfaces.Repositories.IAgentMessageRepository, Data.Repositories.AgentMessageRepository>();
        services.AddScoped<Core.Interfaces.Repositories.ICollaborationRepository, Data.Repositories.CollaborationRepository>();
        services.AddScoped<Core.Interfaces.Repositories.ICollaborationTaskRepository, Data.Repositories.CollaborationTaskRepository>();
        services.AddScoped<Core.Interfaces.Repositories.ILlmConfigRepository, Data.Repositories.LlmConfigRepository>();
        services.AddScoped<Core.Interfaces.Repositories.IOperationLogRepository, Data.Repositories.OperationLogRepository>();
        services.AddScoped<Core.Interfaces.Repositories.ISystemLogRepository, Data.Repositories.SystemLogRepository>();
        
        return services;
    }
}

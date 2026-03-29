using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.Services;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Skills;

namespace MAFStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICollaborationService, CollaborationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ILlmConfigService, LlmConfigService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<IPermissionService, PermissionService>();
        
        services.AddScoped<IChatClientFactory, ChatClientFactory>();
        services.AddScoped<IChatService, ChatService>();
        
        services.AddScoped<IAgentFactoryService, AgentFactoryService>();
        services.AddScoped<ICollaborationWorkflowService, CollaborationWorkflowService>();
        
        services.AddSingleton<SkillLoader>();
        services.AddScoped<SkillExecutor>();
        
        return services;
    }
}

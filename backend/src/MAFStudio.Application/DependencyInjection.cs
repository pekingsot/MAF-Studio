using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.Services;

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
        
        services.AddScoped<IChatClientFactory, ChatClientFactory>();
        services.AddScoped<IChatService, ChatService>();
        
        return services;
    }
}

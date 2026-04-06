using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.Services;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Skills;
using MAFStudio.Application.Capabilities;

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
        
        services.AddSingleton<CapabilityManager>();
        services.AddScoped<IAgentFactoryService, AgentFactoryService>();
        services.AddScoped<ICollaborationWorkflowService, CollaborationWorkflowService>();
        services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
        
        services.AddSingleton<IWorkflowExecutionService, WorkflowExecutionService>();
        
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IGitService, GitService>();
        
        services.AddSingleton<SkillLoader>();
        services.AddScoped<SkillExecutor>();
        
        services.AddScoped<IGroupChatConclusionService, GroupChatConclusionService>();
        
        return services;
    }
}

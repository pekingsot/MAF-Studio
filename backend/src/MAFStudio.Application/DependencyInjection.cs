using Microsoft.Extensions.DependencyInjection;
using MAFStudio.Core.Configuration;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Application.Services;
using MAFStudio.Application.Interfaces;
using MAFStudio.Application.Skills;
using MAFStudio.Application.Capabilities;
using MAFStudio.Application.Prompts;
using MAFStudio.Application.Services.Rag;
using Microsoft.Extensions.Configuration;

namespace MAFStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkspaceOptions>(configuration.GetSection(WorkspaceOptions.SectionName));
        
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICollaborationService, CollaborationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ILlmConfigService, LlmConfigService>();
        services.AddScoped<IOperationLogService, OperationLogService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<IPermissionService, PermissionService>();
        
        services.AddSingleton<ITaskContextService, TaskContextService>();
        
        services.AddScoped<IChatClientFactory, ChatClientFactory>();
        services.AddScoped<IChatService, ChatService>();
        
        services.AddSingleton<CapabilityManager>();
        services.AddScoped<IAgentFactoryService, AgentFactoryService>();
        services.AddScoped<ICollaborationWorkflowService, CollaborationWorkflowService>();
        services.AddScoped<IWorkflowTemplateService, WorkflowTemplateService>();
        
        services.AddSingleton<IWorkflowExecutionService, WorkflowExecutionService>();
        
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IGitService, GitService>();
        
        services.AddScoped<SkillLoader>();
        services.AddScoped<SkillExecutor>();
        
        services.AddScoped<IGroupChatConclusionService, GroupChatConclusionService>();
        
        services.AddScoped<IWorkflowEventProcessor, WorkflowEventProcessor>();
        
        services.AddScoped<ISystemPromptBuilderFactory, SystemPromptBuilderFactory>();
        
        services.AddScoped<IEmailService, EmailService>();
        
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IRerankService, RerankService>();
        services.AddScoped<IVectorStoreService, VectorStoreService>();
        services.AddScoped<ITextSplitterService, TextSplitterService>();
        services.AddScoped<IRagService, RagService>();
        
        return services;
    }
}

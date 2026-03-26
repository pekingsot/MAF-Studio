using Microsoft.AspNetCore.SignalR;

namespace MAFStudio.Api.Hubs;

public class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinCollaboration(Guid collaborationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"collaboration_{collaborationId}");
        _logger.LogInformation("用户 {ConnectionId} 加入协作 {CollaborationId}", Context.ConnectionId, collaborationId);
    }

    public async Task LeaveCollaboration(Guid collaborationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"collaboration_{collaborationId}");
        _logger.LogInformation("用户 {ConnectionId} 离开协作 {CollaborationId}", Context.ConnectionId, collaborationId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("客户端连接: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("客户端断开连接: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

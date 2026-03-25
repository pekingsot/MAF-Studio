using Microsoft.AspNetCore.SignalR;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Data;

namespace MAFStudio.Backend.Hubs
{
    public class AgentHub : Hub
    {
        private readonly IAgentService _agentService;
        private readonly IMessageService _messageService;
        private readonly ICollaborationService _collaborationService;

        public AgentHub(
            IAgentService agentService,
            IMessageService messageService,
            ICollaborationService collaborationService)
        {
            _agentService = agentService;
            _messageService = messageService;
            _collaborationService = collaborationService;
        }

        public async Task JoinAgentGroup(Guid agentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Agent_{agentId}");
            await Clients.OthersInGroup($"Agent_{agentId}").SendAsync("AgentJoined", agentId);
        }

        public async Task LeaveAgentGroup(Guid agentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Agent_{agentId}");
            await Clients.OthersInGroup($"Agent_{agentId}").SendAsync("AgentLeft", agentId);
        }

        public async Task SendMessage(Guid fromAgentId, Guid toAgentId, string content, string type)
        {
            var messageType = Enum.Parse<MessageType>(type);
            var message = await _messageService.SendMessageAsync(fromAgentId, toAgentId, content, messageType);
            
            await Clients.Group($"Agent_{toAgentId}").SendAsync("ReceiveMessage", message);
            await Clients.Caller.SendAsync("MessageSent", message);
        }

        public async Task BroadcastMessage(Guid fromAgentId, string content)
        {
            await Clients.All.SendAsync("BroadcastReceived", fromAgentId, content);
        }

        public async Task UpdateAgentStatus(Guid agentId, string status)
        {
            var agentStatus = Enum.Parse<AgentStatus>(status);
            var agent = await _agentService.UpdateAgentStatusAsync(agentId, agentStatus);
            
            if (agent != null)
            {
                await Clients.All.SendAsync("AgentStatusUpdated", agent);
            }
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
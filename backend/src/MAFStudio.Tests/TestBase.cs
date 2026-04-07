using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Enums;
using MAFStudio.Core.Interfaces.Repositories;
using Xunit;

namespace MAFStudio.Tests;

public abstract class TestBase : IDisposable
{
    protected Mock<IAgentRepository> MockAgentRepository { get; private set; }
    protected Mock<IUserRepository> MockUserRepository { get; private set; }
    protected Mock<ICollaborationRepository> MockCollaborationRepository { get; private set; }
    protected Mock<ICollaborationTaskRepository> MockCollaborationTaskRepository { get; private set; }
    protected Mock<ILlmConfigRepository> MockLlmConfigRepository { get; private set; }
    protected Mock<ISystemLogRepository> MockSystemLogRepository { get; private set; }
    protected Mock<IOperationLogRepository> MockOperationLogRepository { get; private set; }

    protected TestBase()
    {
        MockAgentRepository = new Mock<IAgentRepository>();
        MockUserRepository = new Mock<IUserRepository>();
        MockCollaborationRepository = new Mock<ICollaborationRepository>();
        MockCollaborationTaskRepository = new Mock<ICollaborationTaskRepository>();
        MockLlmConfigRepository = new Mock<ILlmConfigRepository>();
        MockSystemLogRepository = new Mock<ISystemLogRepository>();
        MockOperationLogRepository = new Mock<IOperationLogRepository>();
    }

    protected User CreateTestUser(long id = 1000000000000001, string username = "testuser", string email = "test@test.com", string role = "user")
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    protected Agent CreateTestAgent(string name, long userId, long? llmConfigId = null)
    {
        return new Agent
        {
            Name = name,
            Description = $"Test agent {name}",
            Type = "Assistant",
            SystemPrompt = "Test system prompt",
            UserId = userId,
            Status = AgentStatus.Inactive,
            LlmConfigId = llmConfigId
        };
    }

    protected Collaboration CreateTestCollaboration(string name, long userId)
    {
        return new Collaboration
        {
            Name = name,
            Description = $"Test collaboration {name}",
            Status = CollaborationStatus.Active,
            UserId = userId,
        };
    }

    protected CollaborationAgent CreateCollaborationAgent(long collaborationId, long agentId, string? role = null)
    {
        return new CollaborationAgent
        {
            CollaborationId = collaborationId,
            AgentId = agentId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    protected CollaborationTask CreateTestTask(long collaborationId, string title, string? prompt = null)
    {
        return new CollaborationTask
        {
            CollaborationId = collaborationId,
            Title = title,
            Prompt = prompt,
            Status = CollaborationTaskStatus.Pending,
        };
    }

    protected LlmConfig CreateTestLlmConfig(string name, long userId)
    {
        return new LlmConfig
        {
            Name = name,
            Provider = "OpenAI",
            ApiKey = "test-api-key",
            UserId = userId,
        };
    }

    public void Dispose()
    {
    }
}

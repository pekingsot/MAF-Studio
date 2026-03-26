using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MAFStudio.Backend.Data;
using Xunit;

namespace MAFStudio.Backend.Tests;

public abstract class TestBase : IDisposable
{
    protected ApplicationDbContext DbContext { get; private set; }

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    protected async Task<User> CreateTestUserAsync(string username = "testuser", string email = "test@test.com", string role = "user")
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    protected async Task<Agent> CreateTestAgentAsync(string name, string userId, Guid? llmConfigId = null)
    {
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Test agent {name}",
            Type = "Assistant",
            Configuration = "{}",
            UserId = userId,
            Status = AgentStatus.Inactive,
            CreatedAt = DateTime.UtcNow,
            LLMConfigId = llmConfigId
        };
        DbContext.Agents.Add(agent);
        await DbContext.SaveChangesAsync();
        return agent;
    }

    protected async Task<Collaboration> CreateTestCollaborationAsync(string name, string userId)
    {
        var collaboration = new Collaboration
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Test collaboration {name}",
            Status = CollaborationStatus.Active,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Collaborations.Add(collaboration);
        await DbContext.SaveChangesAsync();
        return collaboration;
    }

    protected async Task<CollaborationAgent> AddAgentToCollaborationAsync(Guid collaborationId, Guid agentId, string? role = null)
    {
        var collaborationAgent = new CollaborationAgent
        {
            Id = Guid.NewGuid(),
            CollaborationId = collaborationId,
            AgentId = agentId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
        DbContext.CollaborationAgents.Add(collaborationAgent);
        await DbContext.SaveChangesAsync();
        return collaborationAgent;
    }

    protected async Task<CollaborationTask> CreateTestTaskAsync(Guid collaborationId, string title)
    {
        var task = new CollaborationTask
        {
            Id = Guid.NewGuid(),
            CollaborationId = collaborationId,
            Title = title,
            Status = Data.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.CollaborationTasks.Add(task);
        await DbContext.SaveChangesAsync();
        return task;
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }
}

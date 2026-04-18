using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Infrastructure.Data;
using MAFStudio.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;
using Dapper;
using Npgsql;

namespace MAFStudio.Tests.Repositories;

public class CoordinationRepositoryTests : IAsyncLifetime
{
    private readonly string _connectionString = "Host=192.168.1.250;Port=5433;Database=mafstudio;Username=pekingsot;Password=sunset@123";
    private readonly IDapperContext _context;
    private long _testCollaborationId;
    private long _testAgentId;

    public CoordinationRepositoryTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            })
            .Build();
        
        _context = new DapperContext(configuration);
    }

    public async Task InitializeAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        _testCollaborationId = await connection.QueryFirstOrDefaultAsync<long>(
            "SELECT id FROM collaborations LIMIT 1");
        
        _testAgentId = await connection.QueryFirstOrDefaultAsync<long>(
            "SELECT id FROM agents LIMIT 1");
        
        if (_testCollaborationId == 0)
        {
            throw new InvalidOperationException("数据库中没有协作记录，无法运行测试");
        }
        
        if (_testAgentId == 0)
        {
            throw new InvalidOperationException("数据库中没有智能体记录，无法运行测试");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateSession_ShouldReturnSessionWithId()
    {
        var repository = new CoordinationSessionRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "running",
            Topic = "测试协调会话",
            StartTime = DateTime.UtcNow
        };

        var result = await repository.CreateAsync(session);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Manager", result.OrchestrationMode);
        Assert.Equal("running", result.Status);

        Console.WriteLine($"[DEBUG] 创建会话成功, ID: {result.Id}");
    }

    [Fact]
    public async Task GetSessionById_ShouldReturnSession()
    {
        var repository = new CoordinationSessionRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "RoundRobin",
            Status = "running",
            Topic = "测试查询",
            StartTime = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(session);
        var result = await repository.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("RoundRobin", result.OrchestrationMode);

        Console.WriteLine($"[DEBUG] 查询会话成功, ID: {result.Id}");
    }

    [Fact]
    public async Task UpdateSession_ShouldUpdateStatus()
    {
        var repository = new CoordinationSessionRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Intelligent",
            Status = "running",
            Topic = "测试更新",
            StartTime = DateTime.UtcNow
        };

        var created = await repository.CreateAsync(session);
        
        created.Status = "completed";
        created.EndTime = DateTime.UtcNow;
        created.TotalRounds = 5;
        created.TotalMessages = 10;
        created.Conclusion = "测试结论";

        var result = await repository.UpdateAsync(created);

        Assert.NotNull(result);
        Assert.Equal("completed", result.Status);
        Assert.Equal(5, result.TotalRounds);
        Assert.Equal(10, result.TotalMessages);
        Assert.NotNull(result.EndTime);

        Console.WriteLine($"[DEBUG] 更新会话成功, 状态: {result.Status}");
    }

    [Fact]
    public async Task CreateRound_ShouldReturnRoundWithId()
    {
        var sessionRepo = new CoordinationSessionRepository(_context);
        var roundRepo = new CoordinationRoundRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "running",
            Topic = "测试轮次",
            StartTime = DateTime.UtcNow
        };

        var createdSession = await sessionRepo.CreateAsync(session);

        var round = new CoordinationRound
        {
            SessionId = createdSession.Id,
            RoundNumber = 1,
            SpeakerName = "产品经理",
            SpeakerRole = "Manager",
            MessageContent = "这是测试消息内容"
        };

        var result = await roundRepo.CreateAsync(round);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(1, result.RoundNumber);
        Assert.Equal("产品经理", result.SpeakerName);

        Console.WriteLine($"[DEBUG] 创建轮次成功, ID: {result.Id}");
    }

    [Fact]
    public async Task GetRoundsBySessionId_ShouldReturnRounds()
    {
        var sessionRepo = new CoordinationSessionRepository(_context);
        var roundRepo = new CoordinationRoundRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "running",
            Topic = "测试轮次列表",
            StartTime = DateTime.UtcNow
        };

        var createdSession = await sessionRepo.CreateAsync(session);

        await roundRepo.CreateAsync(new CoordinationRound
        {
            SessionId = createdSession.Id,
            RoundNumber = 1,
            SpeakerName = "产品经理",
            MessageContent = "消息1"
        });

        await roundRepo.CreateAsync(new CoordinationRound
        {
            SessionId = createdSession.Id,
            RoundNumber = 2,
            SpeakerName = "研发",
            MessageContent = "消息2"
        });

        var rounds = await roundRepo.GetBySessionIdAsync(createdSession.Id);

        Assert.NotNull(rounds);
        Assert.Equal(2, rounds.Count);
        Assert.Equal(1, rounds[0].RoundNumber);
        Assert.Equal(2, rounds[1].RoundNumber);

        Console.WriteLine($"[DEBUG] 查询轮次列表成功, 数量: {rounds.Count}");
    }

    [Fact]
    public async Task CreateParticipant_ShouldReturnParticipantWithId()
    {
        var sessionRepo = new CoordinationSessionRepository(_context);
        var participantRepo = new CoordinationParticipantRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "running",
            Topic = "测试参与者",
            StartTime = DateTime.UtcNow
        };

        var createdSession = await sessionRepo.CreateAsync(session);

        var participant = new CoordinationParticipant
        {
            SessionId = createdSession.Id,
            AgentId = _testAgentId,
            AgentName = "产品经理",
            AgentRole = "Manager",
            IsManager = true
        };

        var result = await participantRepo.CreateAsync(participant);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("产品经理", result.AgentName);
        Assert.True(result.IsManager);

        Console.WriteLine($"[DEBUG] 创建参与者成功, ID: {result.Id}");
    }

    [Fact]
    public async Task IncrementSpeakCount_ShouldIncreaseCount()
    {
        var sessionRepo = new CoordinationSessionRepository(_context);
        var participantRepo = new CoordinationParticipantRepository(_context);
        
        var session = new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "running",
            Topic = "测试发言次数",
            StartTime = DateTime.UtcNow
        };

        var createdSession = await sessionRepo.CreateAsync(session);

        var participant = await participantRepo.CreateAsync(new CoordinationParticipant
        {
            SessionId = createdSession.Id,
            AgentId = _testAgentId,
            AgentName = "测试Agent",
            SpeakCount = 0
        });

        await participantRepo.IncrementSpeakCountAsync(createdSession.Id, _testAgentId);
        await participantRepo.IncrementSpeakCountAsync(createdSession.Id, _testAgentId);
        await participantRepo.IncrementSpeakCountAsync(createdSession.Id, _testAgentId);

        var participants = await participantRepo.GetBySessionIdAsync(createdSession.Id);
        var updated = participants.FirstOrDefault(p => p.AgentId == _testAgentId);

        Assert.NotNull(updated);
        Assert.Equal(3, updated.SpeakCount);

        Console.WriteLine($"[DEBUG] 发言次数更新成功, 次数: {updated.SpeakCount}");
    }

    [Fact]
    public async Task GetSessionsByCollaborationId_ShouldReturnSessions()
    {
        var repository = new CoordinationSessionRepository(_context);
        
        var session1 = await repository.CreateAsync(new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "Manager",
            Status = "completed",
            Topic = "测试会话1",
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = DateTime.UtcNow
        });

        var session2 = await repository.CreateAsync(new CoordinationSession
        {
            CollaborationId = _testCollaborationId,
            OrchestrationMode = "RoundRobin",
            Status = "running",
            Topic = "测试会话2",
            StartTime = DateTime.UtcNow
        });

        var sessions = await repository.GetByCollaborationIdAsync(_testCollaborationId, 100);

        Assert.NotNull(sessions);
        Assert.True(sessions.Count >= 2);
        Assert.Contains(sessions, s => s.Id == session1.Id);
        Assert.Contains(sessions, s => s.Id == session2.Id);

        Console.WriteLine($"[DEBUG] 查询协作会话列表成功, 数量: {sessions.Count}");
    }
}

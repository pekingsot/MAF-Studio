using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MAFStudio.Api.Controllers;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using Xunit;

namespace MAFStudio.Tests.Controllers;

public class CoordinationControllerTests
{
    private readonly Mock<IWorkflowSessionRepository> _mockSessionRepo;
    private readonly Mock<IMessageRepository> _mockMessageRepo;
    private readonly CoordinationController _controller;

    public CoordinationControllerTests()
    {
        _mockSessionRepo = new Mock<IWorkflowSessionRepository>();
        _mockMessageRepo = new Mock<IMessageRepository>();

        var logger = new Mock<ILogger<CoordinationController>>().Object;
        _controller = new CoordinationController(
            _mockSessionRepo.Object,
            _mockMessageRepo.Object,
            logger
        );
    }

    [Fact]
    public async Task GetSessions_ShouldReturnOkWithSessions()
    {
        var collaborationId = 1L;
        var sessions = new List<WorkflowSession>
        {
            new WorkflowSession
            {
                Id = 1,
                CollaborationId = collaborationId,
                WorkflowType = "GroupChat",
                OrchestrationMode = "Manager",
                Status = "completed",
                Topic = "测试会话1"
            },
            new WorkflowSession
            {
                Id = 2,
                CollaborationId = collaborationId,
                WorkflowType = "GroupChat",
                OrchestrationMode = "RoundRobin",
                Status = "running",
                Topic = "测试会话2"
            }
        };

        _mockSessionRepo
            .Setup(r => r.GetByCollaborationIdAsync(collaborationId, 20))
            .ReturnsAsync(sessions);

        var result = await _controller.GetSessions(collaborationId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSessions = Assert.IsType<List<WorkflowSession>>(okResult.Value);
        Assert.Equal(2, returnedSessions.Count);
    }

    [Fact]
    public async Task GetSession_ExistingId_ShouldReturnOkWithSession()
    {
        var sessionId = 1L;
        var session = new WorkflowSession
        {
            Id = sessionId,
            CollaborationId = 1,
            WorkflowType = "GroupChat",
            OrchestrationMode = "Manager",
            Status = "completed",
            Topic = "测试会话"
        };

        _mockSessionRepo
            .Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        var result = await _controller.GetSession(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSession = Assert.IsType<WorkflowSession>(okResult.Value);
        Assert.Equal(sessionId, returnedSession.Id);
    }

    [Fact]
    public async Task GetSession_NonExistingId_ShouldReturnNotFound()
    {
        var sessionId = 999L;

        _mockSessionRepo
            .Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync((WorkflowSession?)null);

        var result = await _controller.GetSession(sessionId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOkWithMessages()
    {
        var sessionId = 1L;
        var messages = new List<Message>
        {
            new Message
            {
                Id = 1,
                SessionId = sessionId,
                MessageType = "coordination",
                RoundNumber = 1,
                FromAgentName = "产品经理",
                Content = "消息1"
            },
            new Message
            {
                Id = 2,
                SessionId = sessionId,
                MessageType = "coordination",
                RoundNumber = 2,
                FromAgentName = "研发",
                Content = "消息2"
            }
        };

        _mockMessageRepo
            .Setup(r => r.GetBySessionIdAsync(sessionId, 1000))
            .ReturnsAsync(messages);

        var result = await _controller.GetMessages(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMessages = Assert.IsType<List<Message>>(okResult.Value);
        Assert.Equal(2, returnedMessages.Count);
    }

    [Fact]
    public async Task GetSessionDetail_ExistingId_ShouldReturnOkWithDetail()
    {
        var sessionId = 1L;
        var session = new WorkflowSession
        {
            Id = sessionId,
            CollaborationId = 1,
            WorkflowType = "GroupChat",
            OrchestrationMode = "Manager",
            Status = "completed",
            Topic = "测试会话"
        };

        var messages = new List<Message>
        {
            new Message
            {
                Id = 1,
                SessionId = sessionId,
                MessageType = "coordination",
                RoundNumber = 1,
                FromAgentName = "产品经理",
                Content = "消息1"
            }
        };

        _mockSessionRepo
            .Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _mockMessageRepo
            .Setup(r => r.GetBySessionIdAsync(sessionId, 1000))
            .ReturnsAsync(messages);

        var result = await _controller.GetSessionDetail(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var detail = okResult.Value;
        Assert.NotNull(detail);
    }

    [Fact]
    public async Task GetSessionDetail_NonExistingId_ShouldReturnNotFound()
    {
        var sessionId = 999L;

        _mockSessionRepo
            .Setup(r => r.GetByIdAsync(sessionId))
            .ReturnsAsync((WorkflowSession?)null);

        var result = await _controller.GetSessionDetail(sessionId);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}

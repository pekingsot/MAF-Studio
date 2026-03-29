using Moq;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Application.Services;
using Xunit;

namespace MAFStudio.Tests.Services;

/// <summary>
/// 权限管理服务测试
/// </summary>
public class PermissionServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _permissionService = new PermissionService(_mockRoleRepository.Object, _mockPermissionRepository.Object);
    }

    [Fact]
    public async Task GetUserRolesAsync_ShouldReturnUserRoles()
    {
        // Arrange
        var userId = 1000000000000001L;
        var expectedRoles = new List<Role>
        {
            new Role { Id = 1000000000000001L, Name = "超级管理员", Code = "SUPER_ADMIN" },
            new Role { Id = 1000000000000002L, Name = "管理员", Code = "ADMIN" }
        };

        _mockRoleRepository.Setup(r => r.GetUserRolesAsync(userId))
            .ReturnsAsync(expectedRoles);

        // Act
        var result = await _permissionService.GetUserRolesAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("SUPER_ADMIN", result[0].Code);
        Assert.Equal("ADMIN", result[1].Code);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnUserPermissions()
    {
        // Arrange
        var userId = 1000000000000001L;
        var expectedPermissions = new List<Permission>
        {
            new Permission { Id = 1000000000000001L, Name = "查看智能体", Code = "agent:read" },
            new Permission { Id = 1000000000000002L, Name = "创建智能体", Code = "agent:create" }
        };

        _mockPermissionRepository.Setup(r => r.GetUserPermissionsAsync(userId))
            .ReturnsAsync(expectedPermissions);

        // Act
        var result = await _permissionService.GetUserPermissionsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("agent:read", result[0].Code);
        Assert.Equal("agent:create", result[1].Code);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_ShouldAssignRole()
    {
        // Arrange
        var userId = 1000000000000001L;
        var roleId = 1000000000000002L;

        _mockRoleRepository.Setup(r => r.AssignRoleToUserAsync(userId, roleId))
            .ReturnsAsync(true);

        // Act
        var result = await _permissionService.AssignRoleToUserAsync(userId, roleId);

        // Assert
        Assert.True(result);
        _mockRoleRepository.Verify(r => r.AssignRoleToUserAsync(userId, roleId), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_ShouldRemoveRole()
    {
        // Arrange
        var userId = 1000000000000001L;
        var roleId = 1000000000000002L;

        _mockRoleRepository.Setup(r => r.RemoveRoleFromUserAsync(userId, roleId))
            .ReturnsAsync(true);

        // Act
        var result = await _permissionService.RemoveRoleFromUserAsync(userId, roleId);

        // Assert
        Assert.True(result);
        _mockRoleRepository.Verify(r => r.RemoveRoleFromUserAsync(userId, roleId), Times.Once);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnAllRoles()
    {
        // Arrange
        var expectedRoles = new List<Role>
        {
            new Role { Id = 1000000000000001L, Name = "超级管理员", Code = "SUPER_ADMIN" },
            new Role { Id = 1000000000000002L, Name = "管理员", Code = "ADMIN" },
            new Role { Id = 1000000000000003L, Name = "普通用户", Code = "USER" }
        };

        _mockRoleRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(expectedRoles);

        // Act
        var result = await _permissionService.GetAllRolesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnAllPermissions()
    {
        // Arrange
        var expectedPermissions = new List<Permission>
        {
            new Permission { Id = 1000000000000001L, Name = "查看智能体", Code = "agent:read" },
            new Permission { Id = 1000000000000002L, Name = "创建智能体", Code = "agent:create" }
        };

        _mockPermissionRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(expectedPermissions);

        // Act
        var result = await _permissionService.GetAllPermissionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}

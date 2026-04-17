using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Core.Interfaces.Repositories;
using MAFStudio.Api.Extensions;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetAllUsers()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role != "ADMIN")
        {
            return StatusCode(403, new { message = "仅管理员可查看用户列表" });
        }

        var users = await _userRepository.GetAllAsync();

        var result = users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            avatar = u.Avatar,
            role = u.Role,
            createdAt = u.CreatedAt,
            updatedAt = u.UpdatedAt
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserById(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            email = user.Email,
            avatar = user.Avatar,
            role = user.Role,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Models.Requests;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CollaborationsController : ControllerBase
    {
        private readonly ICollaborationService _collaborationService;

        public CollaborationsController(ICollaborationService collaborationService)
        {
            _collaborationService = collaborationService;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<ActionResult<List<Collaboration>>> GetAllCollaborations()
        {
            var userId = GetUserId();
            var collaborations = await _collaborationService.GetAllCollaborationsAsync(userId);
            return Ok(collaborations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Collaboration>> GetCollaboration(Guid id)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.GetCollaborationByIdAsync(id, userId);
            if (collaboration == null)
            {
                return NotFound();
            }
            return Ok(collaboration);
        }

        [HttpPost]
        public async Task<ActionResult<Collaboration>> CreateCollaboration([FromBody] CreateCollaborationRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.CreateCollaborationAsync(
                request.Name,
                request.Description,
                request.Path,
                request.GitRepositoryUrl,
                request.GitBranch,
                request.GitUsername,
                request.GitEmail,
                request.GitAccessToken,
                userId
            );
            return CreatedAtAction(nameof(GetCollaboration), new { id = collaboration.Id }, collaboration);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Collaboration>> UpdateCollaboration(Guid id, [FromBody] CreateCollaborationRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.UpdateCollaborationAsync(
                id,
                request.Name,
                request.Description,
                request.Path,
                request.GitRepositoryUrl,
                request.GitBranch,
                request.GitUsername,
                request.GitEmail,
                request.GitAccessToken,
                userId
            );
            if (collaboration == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return Ok(collaboration);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCollaboration(Guid id)
        {
            var userId = GetUserId();
            var result = await _collaborationService.DeleteCollaborationAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "协作项目不存在或无权限删除" });
            }
            return NoContent();
        }

        [HttpPost("{id}/agents")]
        public async Task<ActionResult<Collaboration>> AddAgentToCollaboration(Guid id, [FromBody] AddAgentRequest request)
        {
            var userId = GetUserId();
            var collaboration = await _collaborationService.AddAgentToCollaborationAsync(
                id,
                request.AgentId,
                request.Role,
                userId
            );
            if (collaboration == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return Ok(collaboration);
        }

        [HttpDelete("{id}/agents/{agentId}")]
        public async Task<ActionResult> RemoveAgentFromCollaboration(Guid id, Guid agentId)
        {
            var userId = GetUserId();
            var result = await _collaborationService.RemoveAgentFromCollaborationAsync(id, agentId, userId);
            if (!result)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return NoContent();
        }

        [HttpPost("{id}/tasks")]
        public async Task<ActionResult<CollaborationTask>> CreateTask(Guid id, [FromBody] CreateTaskRequest request)
        {
            var userId = GetUserId();
            var task = await _collaborationService.CreateTaskAsync(id, request.Title, request.Description, userId);
            if (task == null)
            {
                return NotFound(new { message = "协作项目不存在或无权限修改" });
            }
            return CreatedAtAction(nameof(GetCollaboration), new { id }, task);
        }

        [HttpPatch("tasks/{taskId}/status")]
        public async Task<ActionResult<CollaborationTask>> UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var userId = GetUserId();
            if (!Enum.TryParse<Data.TaskStatus>(request.Status, out var status))
            {
                return BadRequest(new { message = "无效的任务状态" });
            }
            var task = await _collaborationService.UpdateTaskStatusAsync(taskId, status, userId);
            if (task == null)
            {
                return NotFound(new { message = "任务不存在或无权限修改" });
            }
            return Ok(task);
        }

        [HttpDelete("tasks/{taskId}")]
        public async Task<ActionResult> DeleteTask(Guid taskId)
        {
            var userId = GetUserId();
            var result = await _collaborationService.DeleteTaskAsync(taskId, userId);
            if (!result)
            {
                return NotFound(new { message = "任务不存在或无权限删除" });
            }
            return NoContent();
        }
    }
}

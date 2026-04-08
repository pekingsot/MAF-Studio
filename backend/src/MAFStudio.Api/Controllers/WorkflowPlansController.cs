using MAFStudio.Application.DTOs;
using MAFStudio.Application.Interfaces;
using MAFStudio.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/collaborations/{collaborationId}/[controller]")]
[Authorize]
public class WorkflowPlansController : ControllerBase
{
    private readonly ICollaborationWorkflowService _workflowService;

    public WorkflowPlansController(ICollaborationWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<WorkflowPlanDto>> GeneratePlan(
        long collaborationId,
        [FromBody] GeneratePlanRequest request)
    {
        var userId = User.GetUserId();
        var plan = await _workflowService.GenerateAndSavePlanAsync(
            collaborationId,
            request.Task,
            userId);

        return Ok(plan);
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowPlanDto>>> GetPlans(long collaborationId)
    {
        var plans = await _workflowService.GetPlansByCollaborationAsync(collaborationId);
        return Ok(plans);
    }

    [HttpGet("{planId}")]
    public async Task<ActionResult<WorkflowPlanDto>> GetPlan(long collaborationId, long planId)
    {
        var plan = await _workflowService.GetPlanAsync(planId);
        if (plan == null)
            return NotFound();

        return Ok(plan);
    }

    [HttpPut("{planId}")]
    public async Task<ActionResult<WorkflowPlanDto>> UpdatePlan(
        long collaborationId,
        long planId,
        [FromBody] UpdatePlanRequest request)
    {
        var plan = await _workflowService.UpdatePlanAsync(planId, request.WorkflowDefinition);
        return Ok(plan);
    }

    [HttpPost("{planId}/execute")]
    public async Task<ActionResult<CollaborationResult>> ExecutePlan(
        long collaborationId,
        long planId,
        [FromBody] ExecutePlanRequest request)
    {
        var userId = User.GetUserId();
        var result = await _workflowService.ExecutePlanAsync(planId, request.Input, userId);
        return Ok(result);
    }

    [HttpDelete("{planId}")]
    public async Task<ActionResult> DeletePlan(long collaborationId, long planId)
    {
        var result = await _workflowService.DeletePlanAsync(planId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}

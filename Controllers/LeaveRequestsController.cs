using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;
using LeaveApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController(ILeaveRequestService leaveRequestService) : ControllerBase
{
	private readonly ILeaveRequestService _leaveRequestService = leaveRequestService;

    [HttpPost]
	public async Task<IActionResult> GenerateLeave([FromBody] CreateLeaveRequestDto request)
	{
        var result = await _leaveRequestService.CreateLeave(request);

		return CreatedAtAction(nameof(GetLeaveById), new { id = result.Id }, result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeaveById(int id)
    {
		var result = await _leaveRequestService.GetLeaveById(id);
		return Ok(result);
    }

    [HttpGet]
	public async Task<IActionResult> GetLeaveRequests([FromQuery] int? employeeId,
		[FromQuery] LeaveStatus? status,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
	{
		var result = await _leaveRequestService.GetLeaveRequests(employeeId, status, page, pageSize);
		return Ok(result);
	}

	[HttpPatch("{id:int}/approve")]
	public async Task<IActionResult> ApproveLeaveRequest(int id)
	{
		var leave = await _leaveRequestService.ApproveLeaveRequest(id);
		return Ok(leave);
	}
}

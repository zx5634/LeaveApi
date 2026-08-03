using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;
using LeaveApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
	private readonly ILeaveRequestService _leaveRequestService;
	public LeaveRequestsController(ILeaveRequestService leaveRequestService)
	{
		_leaveRequestService = leaveRequestService;
	}

	[HttpPost]
	public async Task<IActionResult> GenerateLeave([FromBody] CreateLeaveRequestDto request)
	{
        var result = await _leaveRequestService.CreateLeave(request);

		return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);

    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return NotFound();
    }

    [HttpGet]
	public async Task<IActionResult> GetLeaveRequests([FromQuery] int? employeeId,
		[FromQuery] LeaveStatus? status,
        [FromQuery] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
	{
		return NotFound();
	}
}

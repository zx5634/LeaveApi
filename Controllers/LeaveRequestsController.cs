using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;
using LeaveApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LeaveApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LeaveRequestsController(ILeaveRequestService leaveRequestService) : ControllerBase
{
	private readonly ILeaveRequestService _leaveRequestService = leaveRequestService;

    /// <summary>建立假單</summary>
    /// <param name="request">假單建立資訊</param>
    /// <returns>新建立假單的詳細資料</returns>
    /// <response code="201">假單建立成功，並回傳假單詳細資料與查詢該假單的網址</response>
    /// <response code="400">前端輸入欄位格式錯誤或未填寫或結束日期早於開始日期</response>
    /// <response code="404">該員工不存在</response>
    /// <response code="409">該員工在該期間已有假單</response>
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost]
	public async Task<IActionResult> GenerateLeave([FromBody] CreateLeaveRequestDto request)
	{
        var result = await _leaveRequestService.CreateLeave(request);

		return CreatedAtAction(nameof(GetLeaveById), new { id = result.Id }, result);
    }

    /// <summary>取得假單資料</summary>
    /// <param name="id">假單 ID</param>
    /// <response code="200">假單的詳細資訊</response>
    /// <response code="404">不存在該假單 ID</response>
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeaveById(int id)
    {
		var result = await _leaveRequestService.GetLeaveById(id);
		return Ok(result);
    }

    /// <summary>取得假單清單</summary>
    /// <param name="employeeId">員工 Id (非必填)</param>
    /// <param name="status">假單狀態 (非必填)</param>
    /// <param name="page">分頁碼</param>
    /// <param name="pageSize">分頁數量</param>
    /// <response code="200">假單清單</response>
    [ProducesResponseType(typeof(PagedResultDto), StatusCodes.Status200OK)]
    [HttpGet]
	public async Task<IActionResult> GetLeaveRequests([FromQuery] int? employeeId,
		[FromQuery] LeaveStatus? status,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
	{
		var result = await _leaveRequestService.GetLeaveRequests(employeeId, status, page, pageSize);
		return Ok(result);
	}

    /// <summary>簽核假單</summary>
    /// <param name="id">假單 Id</param>
    /// <response code="200">假單資訊</response>
    /// <response code="404">不存在該假單 ID</response>
    /// <response code="409">該假單狀態不為 Pending</response>
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPatch("{id:int}/approve")]
	public async Task<IActionResult> ApproveLeaveRequest(int id)
	{
		var leave = await _leaveRequestService.ApproveLeaveRequest(id);
		return Ok(leave);
	}
}

using LeaveApi.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace LeaveApi.Models.Dtos;

public class CreateLeaveRequestDto
{
    /// <summary>員工 Id</summary>
	/// <example>1</example>
    public int EmployeeId { get; set; }
    /// <summary>假單類別</summary>
	/// <example>Annual</example>
    public LeaveType Type { get; set; }
    /// <summary>假單開始時間</summary>
	/// <example>2026-08-01T18:00:00Z</example>
    public DateTime StartDate { get; set; }
    /// <summary>假單結束時間</summary>
	/// <example>2026-08-03T18:00:00Z</example>
    public DateTime EndDate { get; set; }
    /// <summary>假單申請原因</summary>
	/// <example>Vacation</example>
    public string Reason { get; set; } = string.Empty;
}
using LeaveApi.Data;
using LeaveApi.Middleware;
using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaveApi.Services;

public class LeaveRequestService: ILeaveRequestService
{
    private readonly LeaveDbContext _context;
    public LeaveRequestService(LeaveDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequestDto> CreateLeave(CreateLeaveRequestDto request)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new BadRequestException("結束日期不得早於開始日期。");
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId);
        if (!employeeExists)
        {
            throw new NotFoundException($"找不到 ID 為 {request.EmployeeId} 的員工。");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            StartDate = ToUtc(request.StartDate),
            EndDate = ToUtc(request.EndDate),
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.LeaveRequests.Add(leaveRequest);

        await _context.SaveChangesAsync();

        return new LeaveRequestDto { 
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            Type = leaveRequest.Type,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            CreatedAt = leaveRequest.CreatedAt
        };
    }

    static DateTime ToUtc(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };
}
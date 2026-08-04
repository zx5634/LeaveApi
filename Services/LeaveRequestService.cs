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
            throw new BadRequestException("End date cannot be prior to start date.");
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId);
        if (!employeeExists)
        {
            throw new NotFoundException($"No employee found with ID {request.EmployeeId}.");
        }
        
        var hasConflict = await _context.LeaveRequests.AnyAsync(x => x.EmployeeId == request.EmployeeId && 
        x.StartDate < ToUtc(request.EndDate) && x.EndDate > ToUtc(request.StartDate) && x.Status != LeaveStatus.Rejected);
        if (hasConflict)
        {
            throw new ConflictException($"A leave request already exists for the same time period.");
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

    public async Task<LeaveRequestDto> GetLeaveById(int id)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (leave == null)
        {
            throw new NotFoundException($"ID {id} not found.");
        }

        return new LeaveRequestDto
        {
            Id = leave!.Id,
            EmployeeId = leave.EmployeeId,
            Type = leave.Type,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Reason = leave.Reason,
            Status = leave.Status,
            CreatedAt = leave.CreatedAt
        };
    }

    public async Task<PagedResultDto> GetLeaveRequests(int? employeeId, LeaveStatus? status, int page, int pageSize)
    {
        var query = _context.LeaveRequests.AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(x => x.EmployeeId == employeeId.Value);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new LeaveRequestDto
            {
                Id = x.Id,
                EmployeeId = x.EmployeeId,
                Type = x.Type,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            }).ToArrayAsync();

        return new PagedResultDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<LeaveRequestDto> ApproveLeaveRequest(int id)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (leave == null)
        {
            throw new NotFoundException($"No leave request found with ID {id}.");
        }
        else if (leave!.Status != LeaveStatus.Pending)
        {
            throw new ConflictException($"Leave request status is not pending.");
        }

        leave.Status = LeaveStatus.Approved;
        await _context.SaveChangesAsync();

        return new LeaveRequestDto
        {
            Id = leave!.Id,
            EmployeeId = leave.EmployeeId,
            Type = leave.Type,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Reason = leave.Reason,
            Status = leave.Status,
            CreatedAt = leave.CreatedAt
        };
    }

    static DateTime ToUtc(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };
}
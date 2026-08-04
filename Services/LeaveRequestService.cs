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
            throw new BadRequestException(LeaveErrors.EndBeforeStart);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId);
        if (!employeeExists)
        {
            throw new NotFoundException(LeaveErrors.EmployeeNotFound(request.EmployeeId));
        }

        var start = ToUtc(request.StartDate);
        var end = ToUtc(request.EndDate);
        var hasConflict = await _context.LeaveRequests.AnyAsync(x => x.EmployeeId == request.EmployeeId && 
        x.StartDate < end && x.EndDate > start && x.Status != LeaveStatus.Rejected);
        if (hasConflict)
        {
            throw new ConflictException(LeaveErrors.PeriodOverlap);
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            StartDate = start,
            EndDate = end,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.LeaveRequests.Add(leaveRequest);

        await _context.SaveChangesAsync();

        return MapToDto(leaveRequest);
    }

    public async Task<LeaveRequestDto> GetLeaveById(int id)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (leave == null)
        {
            throw new NotFoundException(LeaveErrors.LeaveNotFound(id));
        }

        return MapToDto(leave);
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
            .Select(x => new LeaveRequestDto    // 不可抽出，換成方法呼叫 EF 就會翻譯不出 SQL，會退化成把整個 entity 撈回記憶體再轉
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
            throw new NotFoundException(LeaveErrors.LeaveNotFound(id));
        }
        else if (leave.Status != LeaveStatus.Pending)
        {
            throw new ConflictException(LeaveErrors.NotPending);
        }

        leave.Status = LeaveStatus.Approved;
        await _context.SaveChangesAsync();

        return MapToDto(leave);
    }

    static DateTime ToUtc(DateTime v) => v.Kind switch
    {
        DateTimeKind.Utc => v,
        DateTimeKind.Local => v.ToUniversalTime(),
        _ => DateTime.SpecifyKind(v, DateTimeKind.Utc)
    };

    private static LeaveRequestDto MapToDto(LeaveRequest x) => new()
    {
        Id = x.Id,
        EmployeeId = x.EmployeeId,
        Type = x.Type,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        Reason = x.Reason,
        Status = x.Status,
        CreatedAt = x.CreatedAt
    };
}
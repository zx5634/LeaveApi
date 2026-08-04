using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;

namespace LeaveApi.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> CreateLeave(CreateLeaveRequestDto request);
    Task<LeaveRequestDto> GetLeaveById(int id);
    Task<PagedResultDto> GetLeaveRequests(int? employeeId, LeaveStatus? status, int page, int pageSize);
    Task<LeaveRequestDto> ApproveLeaveRequest(int id);
}
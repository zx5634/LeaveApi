using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;

namespace LeaveApi.Services;

public interface ILeaveRequestService
{
    Task<LeaveRequestDto> CreateLeave(CreateLeaveRequestDto request);
}
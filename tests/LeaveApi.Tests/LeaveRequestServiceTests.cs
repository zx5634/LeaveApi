using LeaveApi.Middleware;
using LeaveApi.Models.Dtos;
using LeaveApi.Models.Entities;
using LeaveApi.Services;

namespace LeaveApi.Tests;

public class LeaveRequestServiceTests: TestBase
{
    // Arrange: Context 已含 seed 員工
    // Act: CreateLeave
    // Assert: 非 null 且 Status == Pending
    [Fact]
    public async Task GenerateLeave_Success()
    {
        var service = new LeaveRequestService(Context);
        var result = await service.CreateLeave(new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel"
        });
        
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.Pending, result.Status);
    }

    // Arrange: Context 已含 seed 員工
    // Act: CreateLeave
    // Assert: 拋 BadRequestException
    [Fact]
    public async Task GenerateLeave_EndDateMustLaterThanStartDate()
    {
        var service = new LeaveRequestService(Context);
        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 1,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(1),
            Reason = "Travel"
        };
        
        var exception = await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("End date cannot be prior to start date.", exception.Message);
    }

    // Arrange: 確認員工 Id = 1000 不存在
    // Act: CreateLeave
    // Assert: 拋 NotFoundException
    [Fact]
    public async Task GenerateLeave_EmployeeMustExist()
    {
        var checkExist = Context.Employees.Any(x => x.Id == 1000);
        Assert.False(checkExist);
        var service = new LeaveRequestService(Context);
        var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await service.CreateLeave(new CreateLeaveRequestDto
            {
                EmployeeId = 1000,
                Type = LeaveType.Sick,
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(3)
            });
        });

        Assert.Contains("No employee found with ID", exception.Message);
    }

    // Arrange: 塞一筆較小區間的假單
    // Act: CreateLeave
    // Assert: 拋 ConflictException
    [Fact]
    public async Task GenerateLeave_DateOverlap_OldRangeInsideNewRange()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(4),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(5),
            Reason = "Accident"
        };
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("A leave request already exists for the same time period.", exception.Message);
    }

    // Arrange: 先塞一筆假單
    // Act: CreateLeave
    // Assert: 拋 ConflictException
    [Fact]
    public async Task GenerateLeave_DateOverlap_NewEndDate_In_OldRange()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(5),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Accident"
        };
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("A leave request already exists for the same time period.", exception.Message);
    }
    
    // Arrange: 先塞一筆假單
    // Act: CreateLeave
    // Assert: 拋 ConflictException
    [Fact]
    public async Task GenerateLeave_DateOverlap_NewStartDate_In_OldRange()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(2),
            EndDate = DateTime.UtcNow.AddDays(5),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(4),
            EndDate = DateTime.UtcNow.AddDays(7),
            Reason = "Accident"
        };
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("A leave request already exists for the same time period.", exception.Message);
    }

    // Arrange: 先塞一筆 Status 為 Pending 的假單
    // Act: CreateLeave
    // Assert: 拋 ConflictException
    [Fact]
    public async Task GenerateLeave_Conflict_Pending()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Pending
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Accident"
        };
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("A leave request already exists for the same time period.", exception.Message);
    }

    // Arrange: 先塞一筆 Status 為 Approved 的假單
    // Act: CreateLeave
    // Assert: 拋 ConflictException
    [Fact]
    public async Task GenerateLeave_Conflict_Approved()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Accident"
        };
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.CreateLeave(request);
        });

        Assert.Contains("A leave request already exists for the same time period.", exception.Message);
    }

    // Arrange: 先塞一筆假單
    // Act: CreateLeave
    // Assert: 非 null 且 Status == Pending
    [Fact]
    public async Task GenerateLeave_NonOverlappingRange_Success()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(5),
            Reason = "Accident"
        };
        
        var result = await service.CreateLeave(request);
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.Pending, result.Status);
    }

    // Arrange: 先塞一筆 Status 為 Reject 的假單
    // Act: CreateLeave
    // Assert: 非 null 且 Status == Pending
    [Fact]
    public async Task GenerateLeave_SameRange_OldRequestRejected_Success()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Rejected
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Accident"
        };
        
        var result = await service.CreateLeave(request);
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.Pending, result.Status);
    }

    // Arrange: 先塞第一個員工狀態為的 Approved 的假單
    // Act: CreateLeave
    // Assert: 非 null 且 Status == Pending
    [Fact]
    public async Task GenerateLeave_DifferentEmployee_SameRange_Success()
    {
        var service = new LeaveRequestService(Context);
        Context.Add(new LeaveRequest
        {
            EmployeeId = 1,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        });
        await Context.SaveChangesAsync();

        var request = new CreateLeaveRequestDto
        {
            EmployeeId = 2,
            Type = LeaveType.Personal,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Accident"
        };
        
        var result = await service.CreateLeave(request);
        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.Pending, result.Status);
    }

    // Arrange: 先塞一筆狀態為 Approved 的假單
    // Act: ApproveLeaveRequest
    // Assert: 拋 ConflictException
    [Fact]
    public async Task ApproveLeaveRequest_AlreadyApproved_Conflict()
    {
        var service = new LeaveRequestService(Context);
        var request = new LeaveRequest
        {
            EmployeeId = 2,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Approved
        };
        Context.Add(request);
        await Context.SaveChangesAsync();
        
        var exception = await Assert.ThrowsAsync<ConflictException>(async () =>
        {
            await service.ApproveLeaveRequest(request.Id);
        });

        Assert.Contains("Leave request status is not pending.", exception.Message);
    }

    // Arrange: 先塞一筆狀態為 Pending 的假單
    // Act: ApproveLeaveRequest
    // Assert: 非 null 且 status == Approved
    [Fact]
    public async Task ApproveLeaveRequest_Approved_Success()
    {
        var service = new LeaveRequestService(Context);
        var request = new LeaveRequest
        {
            EmployeeId = 1,
            Type = LeaveType.Annual,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(3),
            Reason = "Travel",
            Status = LeaveStatus.Pending
        };
        Context.Add(request);
        await Context.SaveChangesAsync();
        
        var result = await service.ApproveLeaveRequest(request.Id);

        Assert.NotNull(result);
        Assert.Equal(LeaveStatus.Approved, result.Status);
    }
}

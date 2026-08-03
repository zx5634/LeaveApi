using LeaveApi.Models.Entities;

namespace LeaveApi.Models.Dtos
{
    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public LeaveType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public LeaveStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

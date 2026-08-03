using Microsoft.EntityFrameworkCore;
using LeaveApi.Models.Entities;

namespace LeaveApi.Data;

public class LeaveDbContext : DbContext
{
    // 建構子：接收由 Program.cs 注入的資料庫配置選項
    public LeaveDbContext(DbContextOptions<LeaveDbContext> options) : base(options)
    {
    }

    // 宣告資料表（DbSet<T>）
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;

    // 選擇性設定：定義資料表欄位細節
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, Name = "Anna", Department = "Sales", Email = "anna@gmail.com" },
            new Employee { Id = 2, Name = "Bill", Department = "PM", Email = "bill@gmail.com" }
        );

        // 設定 LeaveStatus Enum 在 PostgreSQL 中儲存為字串而非數字
        modelBuilder.Entity<LeaveRequest>()
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<LeaveRequest>()
            .Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
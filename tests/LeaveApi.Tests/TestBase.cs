using LeaveApi.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public abstract class TestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly LeaveDbContext Context;

    protected TestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();     // 保持連線開啟
        var options = new DbContextOptionsBuilder<LeaveDbContext>()
            .UseSqlite(_connection).Options;
        Context = new LeaveDbContext(options);
        Context.Database.EnsureCreated();       // 建立虛擬表
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();      // 連線關閉 → 資料庫消失
    }
}
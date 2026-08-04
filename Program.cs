using LeaveApi.Data;
using LeaveApi.Middleware;
using LeaveApi.Services;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "缺少 ConnectionStrings:DefaultConnection。本機請執行 dotnet user-secrets set，容器請由環境變數注入。");
// 註冊 LeaveDbContext 並指定使用 PostgreSQL
builder.Services.AddDbContext<LeaveDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // 這支 API 不回純文字。移掉之後 OpenAPI 文件就不會把 text/plain 列成可接受的回應型別，
    // 也不必用 [Produces] 去覆蓋協商結果——那會連錯誤回應的 problem+json 一起蓋掉。
    options.OutputFormatters.RemoveType<StringOutputFormatter>();
})
.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
// 讓框架自己產生的錯誤（例如路由未命中）也走 ProblemDetails
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

// 只有執行時真的有 HTTPS 端點才轉向
if (app.Configuration.GetValue("EnableHttpsRedirection", true))
    app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
    // Automatically applies any pending migrations at runtime
    db.Database.Migrate();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

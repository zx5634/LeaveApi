using LeaveApi.Data;
using LeaveApi.Middleware;
using LeaveApi.Services;
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

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
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

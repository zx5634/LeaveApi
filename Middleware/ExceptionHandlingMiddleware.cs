using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // type 的 URI 對齊 ASP.NET Core 內建的預設值，讓自訂錯誤與框架的模型驗證錯誤長得一樣
        var (status, title, type) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict",
                "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred while processing your request.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        // 預期外的例外完整記錄，但不把細節回給呼叫端
        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            Instance = context.Request.Path
        };
        // 與框架的 ProblemDetailsFactory 取值方式一致：優先用 W3C trace context
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

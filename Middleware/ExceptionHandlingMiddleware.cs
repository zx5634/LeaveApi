using System.Net;
using System.Text.Json;

namespace LeaveApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            BadRequestException => (int)HttpStatusCode.BadRequest,  // 400
            NotFoundException => (int)HttpStatusCode.NotFound,      // 404
            _ => (int)HttpStatusCode.InternalServerError            // 500
        };

        var response = new {
            exception.Message,
            context.Response.StatusCode
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
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
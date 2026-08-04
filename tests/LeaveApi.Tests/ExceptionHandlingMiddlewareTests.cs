using System.Text.Json;
using LeaveApi.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeaveApi.Tests;

public class ExceptionHandlingMiddlewareTests
{
    // Arrange: 組一個必定拋出指定例外的 pipeline（例外型別與預期狀態碼由 InlineData 提供）
    // Act: InvokeAsync
    // Assert: Response.StatusCode 等於預期狀態碼
    [Theory]
    [InlineData(typeof(BadRequestException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict)]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError)]
    public async Task InvokeAsync_MapsExceptionToStatusCode(Type exceptionType, int expected)
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw (Exception)Activator.CreateInstance(exceptionType, "test")!,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(expected, context.Response.StatusCode);
    }

    // Arrange: 組一個拋出 NotFoundException 的 pipeline，並替 Response 掛上可讀取的 Body
    // Act: InvokeAsync
    // Assert: 回應為 RFC 7807 ProblemDetails，且中文未被轉成 \uXXXX
    [Fact]
    public async Task InvokeAsync_WritesProblemDetailsWithUnescapedText()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("找不到 ID 為 1 的假單。"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var json = await ReadBodyAsync(context);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", root.GetProperty("type").GetString());
        Assert.Equal("Not Found", root.GetProperty("title").GetString());
        Assert.Equal(StatusCodes.Status404NotFound, root.GetProperty("status").GetInt32());
        Assert.Equal("找不到 ID 為 1 的假單。", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("\\u", json);
    }

    // Arrange: 組一個拋出未預期例外的 pipeline，例外訊息中含敏感內容
    // Act: InvokeAsync
    // Assert: 回應為 500，且不含原始例外訊息
    [Fact]
    public async Task InvokeAsync_UnexpectedException_DoesNotLeakMessage()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Server=db-prod-01;Password=hunter2"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var json = await ReadBodyAsync(context);
        Assert.DoesNotContain("hunter2", json);
        Assert.DoesNotContain("db-prod-01", json);

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("detail", out _));
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}

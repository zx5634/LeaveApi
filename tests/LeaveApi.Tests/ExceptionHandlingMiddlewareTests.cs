using LeaveApi.Middleware;
using Microsoft.AspNetCore.Http;

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
        var context = new DefaultHttpContext();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw (Exception)Activator.CreateInstance(exceptionType, "test")!);

        await middleware.InvokeAsync(context);

        Assert.Equal(expected, context.Response.StatusCode);
    }

    // Arrange: 組一個拋出 NotFoundException 的 pipeline，並替 Response 掛上可讀取的 Body
    // Act: InvokeAsync
    // Assert: 回應為 camelCase JSON，且中文未被轉成 \uXXXX
    [Fact]
    public async Task InvokeAsync_WritesCamelCaseJsonWithUnescapedText()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new NotFoundException("找不到 ID 為 1 的假單。"));

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal("""{"message":"找不到 ID 為 1 的假單。","statusCode":404}""", json);
    }
}

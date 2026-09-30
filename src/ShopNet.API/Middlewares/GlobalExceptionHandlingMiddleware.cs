using System.Net;
using System.Text.Json;
using ShopNet.Application.Common.Models;
using ShopNet.Domain.Exceptions;

namespace ShopNet.API.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý HTTP request: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            BadRequestException badRequest => ((int)HttpStatusCode.BadRequest, badRequest.Message),
            NotFoundException notFound => ((int)HttpStatusCode.NotFound, notFound.Message),
            UnauthorizedException unauthorized => ((int)HttpStatusCode.Unauthorized, unauthorized.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "Đã xảy ra lỗi máy chủ nội bộ. Vui lòng thử lại sau.")
        };

        context.Response.StatusCode = statusCode;

        var result = Result<object>.Failure(message, statusCode);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

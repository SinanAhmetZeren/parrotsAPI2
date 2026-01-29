using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class DbUnavailableException : Exception
{
    public DbUnavailableException(string message, Exception inner) : base(message, inner) { }
}

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted) return;

            // Check if it's our custom exception OR a known infra failure
            if (ex is DbUnavailableException || IsInfrastructureFailure(ex))
            {
                _logger.LogWarning(ex, "Infrastructure failure - returning 503");
                await WriteResponse(context, StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable");
            }
            else
            {
                _logger.LogError(ex, "Unhandled app error - returning 500");
                var msg = _env.IsDevelopment() ? ex.Message : "Internal server error";
                await WriteResponse(context, StatusCodes.Status500InternalServerError, msg);
            }
        }
    }

    private static bool IsInfrastructureFailure(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is SqlException || ex is SocketException || ex is TimeoutException ||
                ex is RetryLimitExceededException || ex is DbUpdateException)
                return true;
            ex = ex.InnerException;
        }
        return false;
    }

    private static async Task WriteResponse(HttpContext context, int code, string msg)
    {
        context.Response.Clear();
        context.Response.StatusCode = code;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = msg }));
    }
}
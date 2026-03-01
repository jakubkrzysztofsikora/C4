using System.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace C4.Shared.Infrastructure.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, type) = exception switch
        {
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2"),
            SecurityException => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Auth exception for {Method} {Path}: {Message}", httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

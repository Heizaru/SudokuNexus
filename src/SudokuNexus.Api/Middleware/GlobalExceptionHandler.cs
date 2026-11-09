using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SudokuNexus.Api.Middleware;

/// <summary>
/// Translates unhandled exceptions (including <see cref="NotImplementedException"/> from learning modules)
/// into structured RFC 7807 <see cref="ProblemDetails"/> JSON responses so the frontend UI can display
/// friendly guidance on which Module TODO needs to be completed next.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is NotImplementedException notImp)
        {
            logger.LogWarning("Learning Module TODO encountered: {Message}", notImp.Message);

            var todoProblem = new ProblemDetails
            {
                Status = StatusCodes.Status501NotImplemented,
                Title = "Learning Module Not Yet Implemented",
                Detail = notImp.Message,
                Type = "https://sudokunexus.dev/errors/module-todo"
            };

            httpContext.Response.StatusCode = StatusCodes.Status501NotImplemented;
            await httpContext.Response.WriteAsJsonAsync(todoProblem, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception occurred.");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

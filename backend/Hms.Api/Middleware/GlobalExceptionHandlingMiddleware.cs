using Hms.Application.Common;
using Hms.Domain.Billing;
using Microsoft.AspNetCore.Mvc;

namespace Hms.Api.Middleware;

/// <summary>Maps application/billing exceptions to consistent ProblemDetails responses.</summary>
public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// Catch-all API boundary. Safe application/domain messages are returned; unexpected
    /// exceptions are logged in full and reduced to a generic 500 response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RequestValidationException validation)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            var problem = new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more request fields are invalid.",
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };
            foreach (var (key, messages) in validation.Errors) problem.Errors[key] = messages;
            await context.Response.WriteAsJsonAsync(problem);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                AppUnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                AppForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
                AppNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                AppConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                DomainRuleException => (StatusCodes.Status409Conflict, "Domain rule violation"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled API exception");
            else
                logger.LogWarning(exception, "Handled API exception: {Message}", exception.Message);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred. Contact the administrator with the correlation id."
                    : exception.Message,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            });
        }
    }
}

using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using OnlineTeacher.Application.Exceptions;

namespace OnlineTeacher.Api.Middleware;

/// <summary>
/// Centralized exception handling producing consistent RFC-7807 ProblemDetails responses.
/// Maps application exceptions to the approved status codes and never leaks database/
/// stack/sensitive details to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _isDevelopment;

    public ExceptionHandlingMiddleware(
        ProblemDetailsFactory problemDetailsFactory,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/problem+json";

        switch (exception)
        {
            case ValidationException validation:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteAsync(context, StatusCodes.Status400BadRequest, "Validation failed", validation.Message);
                break;

            case NotFoundException notFound:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await WriteAsync(context, StatusCodes.Status404NotFound, "Not found", notFound.Message);
                break;

            case DuplicateEmailException duplicateEmail:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", duplicateEmail.Message);
                break;

            case BusinessRuleViolationException businessRule:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                await WriteAsync(context, StatusCodes.Status422UnprocessableEntity, "Business rule violation", businessRule.Message);
                break;

            case TenantMismatchException tenantMismatch:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteAsync(context, StatusCodes.Status403Forbidden, "Forbidden", tenantMismatch.Message);
                break;

            case ConcurrencyException concurrency:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await WriteAsync(context, StatusCodes.Status409Conflict, "Conflicting request", concurrency.Message);
                break;

            case AuthenticationException authentication:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", authentication.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception during request processing.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                var detail = _isDevelopment ? exception.ToString() : "An unexpected error occurred.";
                await WriteAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Server error",
                    detail);
                break;
        }
    }

    private Task WriteAsync(HttpContext context, int status, string title, string detail)
    {
        var problemDetails = _problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode: status,
            title: title,
            detail: detail);

        return context.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType());
    }
}
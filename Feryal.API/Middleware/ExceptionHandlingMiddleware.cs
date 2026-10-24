using Feryal.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Feryal.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(
                ex,
                "Conflict occurred.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status409Conflict,
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized request.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized.");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status404NotFound,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid operation.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred.");

            await WriteResponseAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ex.ToString());
        }
    }
    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType =
            "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = message
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
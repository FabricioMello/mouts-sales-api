using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Common;
using FluentValidation;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, new ApiResponse
            {
                Success = false,
                Message = "Validation Failed",
                Errors = ex.Errors.Select(error => (ValidationErrorDetail)error)
            });
        }
        catch (EntityNotFoundException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status404NotFound, new ApiResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status404NotFound, new ApiResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (BusinessRuleViolationException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status409Conflict, new ApiResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (DomainException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, new ApiResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, new ApiResponse
            {
                Success = false,
                Message = "An unexpected error occurred"
            });
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, int statusCode, ApiResponse response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}

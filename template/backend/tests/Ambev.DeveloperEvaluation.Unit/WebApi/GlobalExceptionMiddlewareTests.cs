using Ambev.DeveloperEvaluation.Domain.Exceptions;
using Ambev.DeveloperEvaluation.WebApi.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi;

public class GlobalExceptionMiddlewareTests
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();

    [Fact(DisplayName = "ValidationException should return 400")]
    public async Task Given_ValidationException_When_InvokeAsync_Then_ShouldReturn400()
    {
        var middleware = CreateMiddleware(_ => throw new ValidationException(
        [
            new ValidationFailure("Field", "Error")
        ]));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact(DisplayName = "EntityNotFoundException should return 404")]
    public async Task Given_EntityNotFoundException_When_InvokeAsync_Then_ShouldReturn404()
    {
        var middleware = CreateMiddleware(_ => throw new EntityNotFoundException("Sale", Guid.NewGuid()));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact(DisplayName = "BusinessRuleViolationException should return 409")]
    public async Task Given_BusinessRuleViolation_When_InvokeAsync_Then_ShouldReturn409()
    {
        var middleware = CreateMiddleware(_ => throw new BusinessRuleViolationException("Already cancelled"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact(DisplayName = "KeyNotFoundException should return 404")]
    public async Task Given_KeyNotFoundException_When_InvokeAsync_Then_ShouldReturn404()
    {
        var middleware = CreateMiddleware(_ => throw new KeyNotFoundException("User not found"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact(DisplayName = "DomainException should return 400")]
    public async Task Given_DomainException_When_InvokeAsync_Then_ShouldReturn400()
    {
        var middleware = CreateMiddleware(_ => throw new DomainException("Invalid data"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact(DisplayName = "Unexpected exception should return 500")]
    public async Task Given_UnexpectedException_When_InvokeAsync_Then_ShouldReturn500()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("Something broke"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new GlobalExceptionMiddleware(next, _logger);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }
}

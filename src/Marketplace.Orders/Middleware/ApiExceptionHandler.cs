using System.Text.Json;
using FluentValidation;
using Marketplace.Orders.Contracts;
using Marketplace.Orders.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Marketplace.Orders.Middleware;

// Único tradutor de exceção para HTTP. Controller e service não têm try/catch.
public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, body) = exception switch
        {
            DomainException domain => (domain.Status, new ApiErrorResponse(ErrorMessages.For(domain.Code))),
            ValidationException validation => (StatusCodes.Status422UnprocessableEntity, ToResponse(validation)),
            _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse(ErrorMessages.For("internal.error"))),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro não tratado em {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body, ct);

        return true;
    }

    private static ApiErrorResponse ToResponse(ValidationException exception)
    {
        var errors = exception.Errors
            .Select(e => new ApiError(ToCamelCase(e.PropertyName), e.ErrorMessage))
            .ToList();

        return new ApiErrorResponse(ErrorMessages.For("validation.failed"), errors);
    }

    // FluentValidation devolve "Items[0].ProductId"; a API fala camelCase.
    private static string ToCamelCase(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}

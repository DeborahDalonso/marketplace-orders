using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Marketplace.Orders.Middleware;

// Roda o validator registrado para cada argumento da action, quando existe.
// Falha vira ValidationException e o ApiExceptionHandler responde 422.
public class ValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid) throw new ValidationException(result.Errors);
        }

        await next();
    }
}

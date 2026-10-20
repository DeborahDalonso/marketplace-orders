using Marketplace.Orders.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Marketplace.Orders.Middleware;

// 401 e 403 não passam pelo IExceptionHandler; o JwtBearer responde direto.
// Estes eventos trocam a resposta vazia pelo envelope do contrato.
public static class JwtBearerEnvelopeEvents
{
    public static JwtBearerEvents Create() => new()
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(ErrorMessages.For("auth.unauthorized")));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse(ErrorMessages.For("auth.forbidden")));
        },
    };
}

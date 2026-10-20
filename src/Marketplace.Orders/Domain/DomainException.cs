namespace Marketplace.Orders.Domain;

// O código vira a mensagem pt-BR no tradutor de exceções (ApiExceptionHandler).
// A mensagem técnica da exceção nunca chega ao cliente.
public class DomainException(string code, int status) : Exception(code)
{
    public string Code { get; } = code;

    public int Status { get; } = status;
}

using FluentValidation;
using Marketplace.Orders.Contracts;

namespace Marketplace.Orders.Application.Validators;

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(i => i.ProductId).NotEmpty().WithMessage("Informe o produto")
            .Must(id => id.StartsWith("prd_")).WithMessage("Id de produto inválido");
        RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantidade precisa ser maior que zero")
            .LessThanOrEqualTo(100).WithMessage("Quantidade máxima por item é 100");
    }
}

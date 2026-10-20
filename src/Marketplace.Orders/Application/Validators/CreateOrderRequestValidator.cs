using FluentValidation;
using Marketplace.Orders.Contracts;

namespace Marketplace.Orders.Application.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private static readonly string[] PaymentMethods = ["pix", "card"];

    public CreateOrderRequestValidator()
    {
        RuleFor(r => r.Items).NotEmpty().WithMessage("Informe ao menos um item");
        RuleForEach(r => r.Items).SetValidator(new OrderItemRequestValidator());
        RuleFor(r => r.PaymentMethod).NotEmpty().WithMessage("Informe a forma de pagamento")
            .Must(m => PaymentMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Forma de pagamento inválida: use pix ou card");
    }
}

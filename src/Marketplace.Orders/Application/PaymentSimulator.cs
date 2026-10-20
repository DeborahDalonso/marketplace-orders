using Microsoft.Extensions.Options;

namespace Marketplace.Orders.Application;

// Não existe gateway de pagamento neste portfólio: a aprovação é sorteada
// com a taxa de PAYMENT_APPROVAL_RATE para a saga ter os dois caminhos.
public class PaymentSimulator(IOptions<PaymentOptions> options, ILogger<PaymentSimulator> logger)
{
    private readonly double _approvalRate = Math.Clamp(options.Value.ApprovalRate, 0, 1);

    public bool Approve(string orderId)
    {
        var approved = Random.Shared.NextDouble() < _approvalRate;
        logger.LogInformation("Pagamento do pedido {OrderId}: {Result}", orderId, approved ? "aprovado" : "rejeitado");

        return approved;
    }
}

using Marketplace.Orders.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Orders.Tests;

public class PaymentSimulatorTests
{
    private static PaymentSimulator WithRate(double rate) =>
        new(Options.Create(new PaymentOptions { ApprovalRate = rate }), NullLogger<PaymentSimulator>.Instance);

    [Fact]
    public void Aprova_sempre_com_taxa_1()
    {
        var simulator = WithRate(1);

        Assert.All(Enumerable.Range(0, 200), _ => Assert.True(simulator.Approve("ord_x")));
    }

    [Fact]
    public void Rejeita_sempre_com_taxa_0()
    {
        var simulator = WithRate(0);

        Assert.All(Enumerable.Range(0, 200), _ => Assert.False(simulator.Approve("ord_x")));
    }

    [Fact]
    public void Trata_taxa_fora_da_faixa_como_limite()
    {
        Assert.True(WithRate(5).Approve("ord_x"));
        Assert.False(WithRate(-1).Approve("ord_x"));
    }
}

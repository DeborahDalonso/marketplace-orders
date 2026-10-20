using Marketplace.Orders.Domain;

namespace Marketplace.Orders.Tests;

public class OrderTests
{
    private static Order NewOrder() => Order.Create("usr_buyer", PaymentMethod.Pix,
    [
        new OrderItem("prd_a", "usr_seller_a", 2, 19990),
        new OrderItem("prd_b", "usr_seller_b", 1, 2990),
    ]);

    [Fact]
    public void Cria_pedido_pendente_com_total_correto()
    {
        var order = NewOrder();

        Assert.StartsWith("ord_", order.Id);
        Assert.Equal(30, order.Id.Length);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2 * 19990 + 2990, order.TotalCents);
    }

    [Fact]
    public void Lista_vendedores_sem_repetir()
    {
        var order = Order.Create("usr_buyer", PaymentMethod.Card,
        [
            new OrderItem("prd_a", "usr_seller_a", 1, 100),
            new OrderItem("prd_b", "usr_seller_a", 1, 100),
            new OrderItem("prd_c", "usr_seller_b", 1, 100),
        ]);

        Assert.Equal(["usr_seller_a", "usr_seller_b"], order.SellerIds);
    }

    [Fact]
    public void Recusa_pedido_sem_itens()
    {
        var ex = Assert.Throws<DomainException>(() => Order.Create("usr_buyer", PaymentMethod.Pix, []));

        Assert.Equal("order.empty", ex.Code);
        Assert.Equal(422, ex.Status);
    }

    [Fact]
    public void Segue_o_caminho_feliz_pending_confirmed_paid()
    {
        var order = NewOrder();

        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);

        order.MarkPaid();
        Assert.Equal(OrderStatus.Paid, order.Status);
    }

    [Fact]
    public void Nao_paga_pedido_que_nao_foi_confirmado()
    {
        var order = NewOrder();

        var ex = Assert.Throws<DomainException>(order.MarkPaid);

        Assert.Equal("order.invalid_transition", ex.Code);
        Assert.Equal(409, ex.Status);
    }

    [Fact]
    public void Nao_confirma_pedido_duas_vezes()
    {
        var order = NewOrder();
        order.Confirm();

        Assert.Throws<DomainException>(order.Confirm);
    }

    [Fact]
    public void Cancela_pendente_e_confirmado()
    {
        var pending = NewOrder();
        pending.Cancel();
        Assert.Equal(OrderStatus.Cancelled, pending.Status);

        var confirmed = NewOrder();
        confirmed.Confirm();
        confirmed.Cancel();
        Assert.Equal(OrderStatus.Cancelled, confirmed.Status);
    }

    [Fact]
    public void Nao_cancela_pedido_pago()
    {
        var order = NewOrder();
        order.Confirm();
        order.MarkPaid();

        var ex = Assert.Throws<DomainException>(order.Cancel);

        Assert.Equal("order.cannot_cancel", ex.Code);
    }

    [Fact]
    public void Nao_cancela_pedido_ja_cancelado()
    {
        var order = NewOrder();
        order.Cancel();

        var ex = Assert.Throws<DomainException>(order.Cancel);

        Assert.Equal("order.already_cancelled", ex.Code);
    }
}

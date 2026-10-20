using Marketplace.Orders.Application.Validators;
using Marketplace.Orders.Contracts;

namespace Marketplace.Orders.Tests;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Fact]
    public void Aceita_pedido_valido()
    {
        var request = new CreateOrderRequest([new OrderItemRequest("prd_01ABC", 2)], "pix");

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Recusa_lista_de_itens_vazia()
    {
        var result = _validator.Validate(new CreateOrderRequest([], "card"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public void Recusa_quantidade_fora_da_faixa(int quantity)
    {
        var result = _validator.Validate(new CreateOrderRequest([new OrderItemRequest("prd_01ABC", quantity)], "pix"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items[0].Quantity");
    }

    [Fact]
    public void Recusa_id_de_produto_sem_prefixo()
    {
        var result = _validator.Validate(new CreateOrderRequest([new OrderItemRequest("usr_01ABC", 1)], "pix"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Items[0].ProductId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("boleto")]
    public void Recusa_forma_de_pagamento_desconhecida(string method)
    {
        var result = _validator.Validate(new CreateOrderRequest([new OrderItemRequest("prd_01ABC", 1)], method));

        Assert.Contains(result.Errors, e => e.PropertyName == "PaymentMethod");
    }

    [Fact]
    public void Aceita_forma_de_pagamento_em_qualquer_caixa()
    {
        var result = _validator.Validate(new CreateOrderRequest([new OrderItemRequest("prd_01ABC", 1)], "CARD"));

        Assert.True(result.IsValid);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.Orders.Tests.Support;

namespace Marketplace.Orders.Tests;

// Sobe a API com WebApplicationFactory e confere status e envelope de cada tipo de erro.
public class ApiExceptionHandlerTests : IClassFixture<OrdersApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly OrdersApiFactory _factory;

    public ApiExceptionHandlerTests(OrdersApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    [Fact]
    public async Task Responde_401_no_envelope_sem_token()
    {
        var response = await _factory.CreateClient().GetAsync("/orders/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await BodyOf(response);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("Autenticação necessária", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Responde_403_no_envelope_para_role_errada()
    {
        var response = await _factory.CreateClientAs(TestTokens.Buyer, "buyer").GetAsync("/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await BodyOf(response);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("Você não tem permissão para esta ação", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Traduz_domain_exception_em_status_e_mensagem()
    {
        var response = await _factory.CreateClientAs(TestTokens.Admin, "admin").GetAsync("/orders/ord_01NAOEXISTE0000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await BodyOf(response);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("Pedido não encontrado", body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Traduz_validacao_em_422_com_lista_de_campos()
    {
        var client = _factory.CreateClientAs(TestTokens.Buyer, "buyer");

        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId = "prd_01ABC", quantity = 0 } },
            paymentMethod = "boleto",
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await BodyOf(response);
        Assert.Equal("Dados inválidos", body.GetProperty("message").GetString());
        var fields = body.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("field").GetString()).ToList();
        Assert.Contains("items[0].quantity", fields);
        Assert.Contains("paymentMethod", fields);
    }

    [Fact]
    public async Task Traduz_json_invalido_em_422()
    {
        var client = _factory.CreateClientAs(TestTokens.Buyer, "buyer");

        var response = await client.PostAsJsonAsync("/orders", new { items = "nao-e-lista", paymentMethod = "pix" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await BodyOf(response);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.NotEmpty(body.GetProperty("errors").EnumerateArray());
    }

    [Fact]
    public async Task Produto_inexistente_vira_422_sem_detalhe_tecnico()
    {
        var client = _factory.CreateClientAs(TestTokens.Buyer, "buyer");

        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId = "prd_01NAOEXISTE0000000000000", quantity = 1 } },
            paymentMethod = "pix",
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await BodyOf(response);
        Assert.Equal("Produto não encontrado no catálogo", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Cria_pedido_e_responde_201_no_envelope()
    {
        var client = _factory.CreateClientAs(TestTokens.Buyer, "buyer");

        var response = await client.PostAsJsonAsync("/orders", new
        {
            items = new[] { new { productId = FakeCatalogHandler.Headphone.Id, quantity = 2 } },
            paymentMethod = "card",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await BodyOf(response);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Equal("Pedido criado", body.GetProperty("message").GetString());
        var data = body.GetProperty("data");
        Assert.Equal("pending", data.GetProperty("status").GetString());
        Assert.Equal("card", data.GetProperty("paymentMethod").GetString());
        Assert.Equal(39980, data.GetProperty("totalCents").GetInt32());
        Assert.Equal(TestTokens.Buyer, data.GetProperty("buyerId").GetString());
    }
}

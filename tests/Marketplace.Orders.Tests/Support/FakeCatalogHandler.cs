using System.Net;
using System.Net.Http.Json;
using Marketplace.Orders.Contracts;
using Marketplace.Orders.Infrastructure.Catalog;

namespace Marketplace.Orders.Tests.Support;

// Substitui o catalog real: responde GET /products/{id} com o que está no dicionário.
public class FakeCatalogHandler(IReadOnlyDictionary<string, CatalogProduct> products) : HttpMessageHandler
{
    public static readonly CatalogProduct Headphone = new("prd_01HEADPHONE00000000000000", "usr_01SELLERA000000000000000", "Fone Bluetooth", 19990, 12, true);
    public static readonly CatalogProduct Cable = new("prd_01CABLE000000000000000000", "usr_01SELLERB000000000000000", "Cabo USB-C", 2990, 50, true);
    public static readonly CatalogProduct Inactive = new("prd_01INACTIVE00000000000000", "usr_01SELLERA000000000000000", "Produto antigo", 1000, 0, false);

    public static FakeCatalogHandler WithDefaults() => new(new Dictionary<string, CatalogProduct>
    {
        [Headphone.Id] = Headphone,
        [Cable.Id] = Cable,
        [Inactive.Id] = Inactive,
    });

    public CatalogClient AsClient() => new(
        new HttpClient(this) { BaseAddress = new Uri("http://catalog.test") },
        Microsoft.Extensions.Logging.Abstractions.NullLogger<CatalogClient>.Instance);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = request.RequestUri!.AbsolutePath.Split('/').Last();
        if (!products.TryGetValue(id, out var product))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new ApiErrorResponse("Produto não encontrado")),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(ApiResponse<CatalogProduct>.Ok(product)),
        });
    }
}

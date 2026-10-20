using System.Net;
using System.Net.Http.Json;
using Marketplace.Orders.Contracts;

namespace Marketplace.Orders.Infrastructure.Catalog;

// HttpClient tipado: a BaseAddress vem de CATALOG_URL e é configurada no Program.
public class CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
{
    /// <summary>Busca um produto no catálogo. Devolve null quando não existe.</summary>
    public async Task<CatalogProduct?> GetProductAsync(string productId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"/products/{productId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Produto {ProductId} não encontrado no catalog", productId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CatalogProduct>>(ct);

        return body?.Data;
    }
}

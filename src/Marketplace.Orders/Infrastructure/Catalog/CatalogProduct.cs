namespace Marketplace.Orders.Infrastructure.Catalog;

public record CatalogProduct(string Id, string SellerId, string Name, int PriceCents, int Stock, bool Active);

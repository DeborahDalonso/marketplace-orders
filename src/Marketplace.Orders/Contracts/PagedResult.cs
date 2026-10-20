namespace Marketplace.Orders.Contracts;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PerPage, int Total);

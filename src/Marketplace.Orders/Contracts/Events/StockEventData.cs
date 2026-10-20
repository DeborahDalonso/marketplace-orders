namespace Marketplace.Orders.Contracts.Events;

// stock.reserved e stock.rejected trazem mais campos; aqui só o orderId interessa.
public record StockEventData(string OrderId);

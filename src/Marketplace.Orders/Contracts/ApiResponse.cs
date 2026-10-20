namespace Marketplace.Orders.Contracts;

public record ApiResponse<T>(bool Success, string? Message, T? Data)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, message, data);
}

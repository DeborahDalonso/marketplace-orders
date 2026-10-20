using System.Text.Json.Serialization;

namespace Marketplace.Orders.Contracts;

public record ApiErrorResponse(string Message, IReadOnlyList<ApiError>? Errors = null)
{
    public bool Success => false;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ApiError>? Errors { get; init; } = Errors;
}

public record ApiError(string Field, string Message);

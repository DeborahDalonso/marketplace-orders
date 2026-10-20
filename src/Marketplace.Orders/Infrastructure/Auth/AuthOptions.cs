namespace Marketplace.Orders.Infrastructure.Auth;

public class AuthOptions
{
    public const string Section = "Auth";

    public string Url { get; set; } = "http://localhost:3003";

    public string Issuer { get; set; } = "marketplace-auth";

    public string JwksUrl => Url.TrimEnd('/') + "/.well-known/jwks.json";
}

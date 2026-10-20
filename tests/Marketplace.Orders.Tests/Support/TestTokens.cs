using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Marketplace.Orders.Tests.Support;

// Par de chaves gerado por execução; o factory injeta a pública no JwtBearer.
public static class TestTokens
{
    public static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public const string Buyer = "usr_01BUYER000000000000000000";
    public const string OtherBuyer = "usr_01OTHER000000000000000000";
    public const string Admin = "usr_01ADMIN000000000000000000";

    public static string For(string userId, string role)
    {
        var token = new JwtSecurityToken(
            issuer: "marketplace-auth",
            claims:
            [
                new Claim("sub", userId),
                new Claim("email", $"{role}@example.com"),
                new Claim("name", role),
                new Claim("role", role),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.RsaSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

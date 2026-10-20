using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Marketplace.Orders.Infrastructure.Auth;

// O auth não expõe OIDC discovery, só o JWKS. Este retriever alimenta o
// ConfigurationManager do JwtBearer direto com as chaves, e o próprio
// manager cuida do cache (10 min) e do refresh quando aparece um kid novo.
public class JwksConfigurationRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var json = await retriever.GetDocumentAsync(address, cancel);
        var jwks = new JsonWebKeySet(json);
        var configuration = new OpenIdConnectConfiguration { JsonWebKeySet = jwks };

        foreach (var key in jwks.GetSigningKeys())
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }
}

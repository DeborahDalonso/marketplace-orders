using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using FluentValidation;
using Marketplace.Orders.Application;
using Marketplace.Orders.Application.Validators;
using Marketplace.Orders.Contracts;
using Marketplace.Orders.Infrastructure.Auth;
using Marketplace.Orders.Infrastructure.Catalog;
using Marketplace.Orders.Infrastructure.Configuration;
using Marketplace.Orders.Infrastructure.Database;
using Marketplace.Orders.Infrastructure.Kafka;
using Marketplace.Orders.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddContractEnvironmentVariables();
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenAnyIP(builder.Configuration.GetValue("Port", 3002)));

// ==== Options ====
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Section));
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.Section));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.Section));
builder.Services.Configure<CatalogOptions>(builder.Configuration.GetSection(CatalogOptions.Section));
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.Section));

// ==== Banco ====
builder.Services.AddDbContext<OrdersDbContext>((services, options) =>
{
    var database = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    options.UseMySql(database.ConnectionString, ServerVersion.Parse("8.4.0-mysql"));
});

// ==== Autenticação ====
var auth = builder.Configuration.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
        auth.JwksUrl, new JwksConfigurationRetriever(), new HttpDocumentRetriever { RequireHttps = false })
    {
        AutomaticRefreshInterval = TimeSpan.FromMinutes(10),
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = auth.Issuer,
        ValidateAudience = false,
        RoleClaimType = "role",
        NameClaimType = "sub",
        ClockSkew = TimeSpan.FromSeconds(30),
    };
    options.Events = JwtBearerEnvelopeEvents.Create();
});
builder.Services.AddAuthorization();

// ==== Serviços ====
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
builder.Services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderSagaService>();
builder.Services.AddSingleton<PaymentSimulator>();
builder.Services.AddHttpClient<CatalogClient>((services, http) =>
{
    http.BaseAddress = new Uri(services.GetRequiredService<IOptions<CatalogOptions>>().Value.Url);
    http.Timeout = TimeSpan.FromSeconds(5);
});

// ==== Kafka ====
builder.Services.AddSingleton<IProducer<string, string>>(services =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = services.GetRequiredService<IOptions<KafkaOptions>>().Value.Brokers,
        Acks = Acks.All,
        EnableIdempotence = true,
    };
    var logger = services.GetRequiredService<ILogger<IProducer<string, string>>>();

    return new ProducerBuilder<string, string>(config)
        .SetErrorHandler((_, error) => logger.LogWarning("Kafka: {Reason}", error.Reason))
        .Build();
});
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<StockEventsConsumer>();

// ==== HTTP ====
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationActionFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)))
    .ConfigureApiBehaviorOptions(options =>
    {
        // Erro de binding do JSON (tipo errado, body vazio) vira 422 no mesmo envelope.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => new ApiError(
                    string.IsNullOrEmpty(entry.Key) ? "body" : entry.Key.TrimStart('$', '.'),
                    "Valor inválido"))
                .ToList();

            return new UnprocessableEntityObjectResult(new ApiErrorResponse(ErrorMessages.For("validation.failed"), errors));
        };
    });

var app = builder.Build();

// O lambda vazio só satisfaz o middleware; quem responde é o ApiExceptionHandler.
app.UseExceptionHandler(_ => { });
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database;

    // As migrations são do MySQL. Nos testes o provider é SQLite e o schema sai do modelo.
    if (database.IsMySql()) database.Migrate();
    else database.EnsureCreated();
}

app.Run();

// Expõe a classe para o WebApplicationFactory dos testes.
public partial class Program
{
}

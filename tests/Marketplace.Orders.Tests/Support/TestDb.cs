using Marketplace.Orders.Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Orders.Tests.Support;

// SQLite em memória vive enquanto a conexão estiver aberta; por isso o
// contexto e a conexão são descartados juntos.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDb()
    {
        _connection.Open();
        Context = CreateContext();
        Context.Database.EnsureCreated();
    }

    public OrdersDbContext Context { get; }

    // Um contexto novo sobre a mesma conexão, para ler sem o cache do primeiro.
    public OrdersDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(_connection).Options);

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

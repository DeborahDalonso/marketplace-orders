using Marketplace.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Orders.Infrastructure.Database;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Id).HasColumnName("id").HasColumnType("char(30)");
            order.Property(o => o.BuyerId).HasColumnName("buyer_id").HasColumnType("char(30)");
            order.Property(o => o.Status).HasColumnName("status").HasMaxLength(20)
                .HasConversion(s => s.ToString().ToLowerInvariant(), s => Enum.Parse<OrderStatus>(s, true));
            order.Property(o => o.TotalCents).HasColumnName("total_cents");
            order.Property(o => o.PaymentMethod).HasColumnName("payment_method").HasMaxLength(10)
                .HasConversion(m => m.ToString().ToLowerInvariant(), m => Enum.Parse<PaymentMethod>(m, true));
            order.Property(o => o.CreatedAt).HasColumnName("created_at");
            order.Property(o => o.UpdatedAt).HasColumnName("updated_at");
            order.Ignore(o => o.SellerIds);
            order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            order.HasIndex(o => o.BuyerId);
            order.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("order_items");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).HasColumnName("id");
            item.Property(i => i.OrderId).HasColumnName("order_id").HasColumnType("char(30)");
            item.Property(i => i.ProductId).HasColumnName("product_id").HasColumnType("char(30)");
            item.Property(i => i.SellerId).HasColumnName("seller_id").HasColumnType("char(30)");
            item.Property(i => i.Quantity).HasColumnName("quantity");
            item.Property(i => i.PriceCents).HasColumnName("price_cents");
            item.Ignore(i => i.SubtotalCents);
            item.HasIndex(i => i.SellerId);
        });

        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.ToTable("outbox");
            outbox.HasKey(m => m.Id);
            outbox.Property(m => m.Id).HasColumnName("id");
            outbox.Property(m => m.EventId).HasColumnName("event_id").HasColumnType("char(30)");
            outbox.Property(m => m.Type).HasColumnName("type").HasMaxLength(50);
            outbox.Property(m => m.AggregateId).HasColumnName("aggregate_id").HasColumnType("char(30)");
            outbox.Property(m => m.Payload).HasColumnName("payload").HasColumnType("json");
            outbox.Property(m => m.CreatedAt).HasColumnName("created_at");
            outbox.Property(m => m.PublishedAt).HasColumnName("published_at");
            outbox.HasIndex(m => m.PublishedAt);
        });

        modelBuilder.Entity<ProcessedEvent>(processed =>
        {
            processed.ToTable("processed_events");
            processed.HasKey(e => e.EventId);
            processed.Property(e => e.EventId).HasColumnName("event_id").HasColumnType("char(30)");
            processed.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
            processed.Property(e => e.ProcessedAt).HasColumnName("processed_at");
        });
    }
}

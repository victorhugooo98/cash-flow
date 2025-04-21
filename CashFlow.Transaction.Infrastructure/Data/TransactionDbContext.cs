using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transaction.Infrastructure.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Models.Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Domain.Models.Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).IsRequired();

            // Create index for merchant ID and timestamp for faster queries
            entity.HasIndex(e => new { e.MerchantId, e.Timestamp });
        });
    }
}
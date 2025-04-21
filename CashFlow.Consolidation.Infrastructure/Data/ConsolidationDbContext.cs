using CashFlow.Consolidation.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Data;

public class ConsolidationDbContext : DbContext
{
    public ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : base(options)
    {
    }
        
    public DbSet<DailyBalance> DailyBalances { get; set; }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
            
        modelBuilder.Entity<DailyBalance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCredits).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalDebits).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18,2)");
                
            // Create unique index for merchant ID and date
            entity.HasIndex(e => new { e.MerchantId, e.Date }).IsUnique();
        });
    }
}
using CashFlow.Consolidation.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Data;

public class ConsolidationDbContext : DbContext
{
    public ConsolidationDbContext(DbContextOptions<ConsolidationDbContext> options) : base(options)
    {
    }

    public DbSet<DailyBalance> DailyBalances { get; set; }
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
    public DbSet<ProcessedTransaction> ProcessedTransactions { get; set; }

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

            entity.Property(e => e.Date).HasColumnType("date");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            // Create unique index for merchant ID and date
            entity.HasIndex(e => new { e.MerchantId, e.Date }).IsUnique();
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.ProcessedAt).IsRequired();
            entity.ToTable("ProcessedMessages");
        });
    }

    public async Task<List<string>> GetActiveDatabaseLocksAsync()
    {
        var locks = new List<string>();

        // Note: This query works for SQL Server
        const string sql = """
                                   SELECT 
                                       DB_NAME(resource_database_id) AS DatabaseName,
                                       OBJECT_NAME(resource_associated_entity_id) AS LockedObjectName,
                                       request_mode AS LockType,
                                       request_status AS LockStatus
                                   FROM sys.dm_tran_locks
                                   WHERE resource_database_id = DB_ID()
                           """;

        using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var objectName = reader["LockedObjectName"].ToString();
            var lockType = reader["LockType"].ToString();
            var lockStatus = reader["LockStatus"].ToString();

            locks.Add($"Object: {objectName}, Type: {lockType}, Status: {lockStatus}");
        }

        return locks;
    }
}
using Microsoft.EntityFrameworkCore;
using PdInventory.Models;

namespace PdInventory.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PdCategory> Categories => Set<PdCategory>();
    public DbSet<Purpose> Purposes => Set<Purpose>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<TransferRecord> TransferRecords => Set<TransferRecord>();
    public DbSet<InfoSystem> InfoSystems => Set<InfoSystem>();
    public DbSet<RiskCategory> RiskCategories => Set<RiskCategory>();
    public DbSet<RiskImpactLevel> RiskImpactLevels => Set<RiskImpactLevel>();
    public DbSet<RiskLikelihoodLevel> RiskLikelihoodLevels => Set<RiskLikelihoodLevel>();
    public DbSet<RiskEffectivenessLevel> RiskEffectivenessLevels => Set<RiskEffectivenessLevel>();
    public DbSet<ExportColumn> ExportColumns => Set<ExportColumn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PdCategory>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Purpose>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<RiskCategory>().HasIndex(r => r.Code).IsUnique();
        modelBuilder.Entity<RiskImpactLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskLikelihoodLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskEffectivenessLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<ExportColumn>().HasIndex(e => new { e.ListKey, e.PropertyName }).IsUnique();

        modelBuilder.Entity<InventoryItem>()
            .HasMany(i => i.Categories)
            .WithMany(c => c.InventoryItems)
            .UsingEntity(j => j.ToTable("InventoryItemCategories"));

        modelBuilder.Entity<InventoryItem>()
            .HasMany(i => i.Purposes)
            .WithMany(p => p.InventoryItems)
            .UsingEntity(j => j.ToTable("InventoryItemPurposes"));
    }
}

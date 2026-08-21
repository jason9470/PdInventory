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

        // 主檔識別欄位唯一。SystemCode 是跨表關聯的鍵（DA↔SW、盤點表與拋轉清單的弱關聯、
        // 以及清單頁的搜尋記憶），重複會讓關聯行為變得不可預期。
        // 以 HasFilter 排除空字串：SQLite 視空字串為相等值，未填代碼的資料會互相衝突。
        modelBuilder.Entity<InfoSystem>().HasIndex(s => s.SystemCode).IsUnique()
            .HasFilter("\"SystemCode\" <> ''");
        modelBuilder.Entity<InventoryItem>().HasIndex(i => i.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> ''");
        modelBuilder.Entity<TransferRecord>().HasIndex(t => t.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> ''");

        // DaAssetCode 刻意不設唯一：來源資料本來就允許一筆 DA 對應多個 SW
        // （例如 DA-049 同時對應 SW-048 與 SW-049）。

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

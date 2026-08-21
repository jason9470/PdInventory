using Microsoft.EntityFrameworkCore;
using PdInventory.Helpers;
using PdInventory.Models;

namespace PdInventory.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
        : base(options) => _currentUser = currentUser;

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
        // 軟刪除的資料一律不出現在任何查詢（清單、檢視、匯出、重複檢查都吃這個篩選）
        modelBuilder.Entity<InfoSystem>().HasQueryFilter(s => !s.IsDeleted);

        // 唯一索引同時排除已刪除，否則刪掉 SW-027 之後就再也不能建立同編號的資料
        modelBuilder.Entity<InfoSystem>().HasIndex(s => s.SystemCode).IsUnique()
            .HasFilter("\"SystemCode\" <> '' AND \"IsDeleted\" = 0");
        modelBuilder.Entity<InventoryItem>().HasIndex(i => i.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> ''");
        modelBuilder.Entity<TransferRecord>().HasIndex(t => t.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> ''");

        // DaAssetCode 刻意不設唯一：來源資料本來就允許一筆 DA 對應多個 SW
        // （例如 DA-049 同時對應 SW-048 與 SW-049）。

        // SQLite 沒有原生 rowversion，改以 Guid 當並行權杖，於 SaveChanges 換新值
        modelBuilder.Entity<InfoSystem>().Property(e => e.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<InventoryItem>().Property(e => e.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<TransferRecord>().Property(e => e.RowVersion).IsConcurrencyToken();

        modelBuilder.Entity<InventoryItem>()
            .HasMany(i => i.Categories)
            .WithMany(c => c.InventoryItems)
            .UsingEntity(j => j.ToTable("InventoryItemCategories"));

        modelBuilder.Entity<InventoryItem>()
            .HasMany(i => i.Purposes)
            .WithMany(p => p.InventoryItems)
            .UsingEntity(j => j.ToTable("InventoryItemPurposes"));
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 寫入建立／異動軌跡並換發並行權杖。集中在這裡處理，控制器不需要記得做，
    /// 也就不會有「某個動作忘了寫軌跡」的漏洞。
    /// </summary>
    private void StampAuditFields()
    {
        var now = DateTime.Now;
        var user = _currentUser.Name;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = user;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = user;
                // 建立資訊不因後續修改而變動
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IConcurrencyAware>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.RowVersion = Guid.NewGuid();
        }
    }
}

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
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AssetOwner> AssetOwners => Set<AssetOwner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PdCategory>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Purpose>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<RiskCategory>().HasIndex(r => r.Code).IsUnique();
        modelBuilder.Entity<RiskImpactLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskLikelihoodLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskEffectivenessLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<ExportColumn>().HasIndex(e => new { e.ListKey, e.PropertyName }).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(u => u.EmpNo).IsUnique();

        // 同一位使用者對同一個資產只會有一筆授權；重複勾選在畫面上看不出來，但會讓
        // 「取消授權」變成刪一筆留一筆的假象，因此由資料庫直接擋掉。
        modelBuilder.Entity<AssetOwner>().HasIndex(a => new { a.AppUserId, a.InfoSystemId }).IsUnique();

        modelBuilder.Entity<AssetOwner>()
            .HasOne(a => a.User).WithMany(u => u.OwnedAssets)
            .HasForeignKey(a => a.AppUserId).OnDelete(DeleteBehavior.Cascade);

        // 資產被刪除時一併移除授權，避免留下指向不存在資產的孤兒列
        modelBuilder.Entity<AssetOwner>()
            .HasOne(a => a.InfoSystem).WithMany()
            .HasForeignKey(a => a.InfoSystemId).OnDelete(DeleteBehavior.Cascade);

        // 授權跟著資產走：資產被軟刪除後，授權也不該再出現在權限畫面或權限判斷中。
        // 若不加這個篩選，EF 會警告「必要導覽指向帶有查詢篩選的實體」，且 Include
        // 後的 InfoSystem 會變成 null，判斷時要到處補 null 檢查。
        modelBuilder.Entity<AssetOwner>().HasQueryFilter(a => !a.InfoSystem!.IsDeleted);

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
        modelBuilder.Entity<AppUser>().Property(e => e.RowVersion).IsConcurrencyToken();

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
        RecalculateAssetValues();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        RecalculateAssetValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 資產價值 = 機密性 ＋ 完整性 ＋ 可用性。
    ///
    /// 原本是人工填寫，但現有 63 筆資料全部剛好等於這個總和，代表它本來就是算出來的，
    /// 讓人重填只會多一個算錯的機會。放在這裡而不是控制器，是因為新增與編輯兩條路徑
    /// 都會寫到這三個等級，集中處理才不會有某一條忘了算。
    ///
    /// 三個等級只要有一個不是數字（例如外購軟體填 N/A）就維持原值不動，
    /// 不去猜使用者的意思。
    /// </summary>
    private void RecalculateAssetValues()
    {
        foreach (var entry in ChangeTracker.Entries<InfoSystem>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            var system = entry.Entity;
            if (int.TryParse(system.SwConfidentiality, out var confidentiality)
                && int.TryParse(system.SwIntegrity, out var integrity)
                && int.TryParse(system.SwAvailability, out var availability))
            {
                system.SwAssetValue = (confidentiality + integrity + availability).ToString();
            }
        }
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

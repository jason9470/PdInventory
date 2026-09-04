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
    public DbSet<DataAsset> DataAssets => Set<DataAsset>();
    public DbSet<SystemInventory> SystemInventories => Set<SystemInventory>();
    public DbSet<RiskCategory> RiskCategories => Set<RiskCategory>();
    public DbSet<RiskImpactLevel> RiskImpactLevels => Set<RiskImpactLevel>();
    public DbSet<RiskLikelihoodLevel> RiskLikelihoodLevels => Set<RiskLikelihoodLevel>();
    public DbSet<RiskEffectivenessLevel> RiskEffectivenessLevels => Set<RiskEffectivenessLevel>();
    public DbSet<ExportColumn> ExportColumns => Set<ExportColumn>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AssetOwner> AssetOwners => Set<AssetOwner>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<OpsStaff> OpsStaffs => Set<OpsStaff>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<FieldOptionItem> FieldOptionItems => Set<FieldOptionItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PdCategory>().HasIndex(c => c.Code).IsUnique();
        modelBuilder.Entity<Purpose>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<RiskCategory>().HasIndex(r => r.Code).IsUnique();
        modelBuilder.Entity<RiskImpactLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskLikelihoodLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<RiskEffectivenessLevel>().HasIndex(r => r.Level).IsUnique();
        modelBuilder.Entity<ExportColumn>().HasIndex(e => new { e.ListKey, e.PropertyName }).IsUnique();
        // 唯一索引排除已停用：同一個人被停用後又回鍋，要能重新建檔而不是撞號
        modelBuilder.Entity<AppUser>().HasIndex(u => u.EmpNo).IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

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

        // 人員與使用者帳號一併採軟刪除：被刪的人不會出現在維護清單、任何下拉，
        // 也不會出現在權限設定，而且無法再登入（見 AccountController）。
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AppUser>().HasQueryFilter(u => !u.IsDeleted);

        // 盤點表與拋轉清單也改成軟刪除，全站的刪除行為因此一致
        modelBuilder.Entity<InventoryItem>().HasQueryFilter(i => !i.IsDeleted);
        modelBuilder.Entity<TransferRecord>().HasQueryFilter(t => !t.IsDeleted);

        // 唯一索引同時排除已刪除，否則刪掉 SW-027 之後就再也不能建立同編號的資料
        modelBuilder.Entity<InfoSystem>().HasIndex(s => s.SystemCode).IsUnique()
            .HasFilter("\"SystemCode\" <> '' AND \"IsDeleted\" = 0");
        modelBuilder.Entity<InventoryItem>().HasIndex(i => i.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> '' AND \"IsDeleted\" = 0");
        modelBuilder.Entity<TransferRecord>().HasIndex(t => t.SeqNo).IsUnique()
            .HasFilter("\"SeqNo\" <> '' AND \"IsDeleted\" = 0");

        // 一套系統只會有一份盤點表；空字串排除在外的寫法同上
        modelBuilder.Entity<SystemInventory>().HasIndex(i => i.SystemCode).IsUnique()
            .HasFilter("\"SystemCode\" <> ''");

        // 共用維護資料：名稱是各表單存進欄位的值，必須唯一，否則下拉會出現兩個一模一樣的選項。
        // 代號（利潤中心／組別）刻意不設唯一——資料裡有、甲方清單沒有的先留空，會有一批空值。
        modelBuilder.Entity<Department>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<OpsStaff>().HasIndex(o => o.Name).IsUnique();
        // 人員是軟刪除，唯一性只在還沒被刪的人之間成立
        // 同一個欄位底下的選項不能重複，否則下拉會出現兩個一模一樣的
        modelBuilder.Entity<FieldOptionItem>().HasIndex(o => new { o.FieldName, o.Value }).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(e => e.Name).IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        // DataAssets.DaAssetCode 刻意不設唯一：來源資料允許一筆 DA 對應多個 SW
        // （DA-049 的關連SW編號是「SW-049;SW-048」）。為了讓 SystemCode 維持單一值
        // ——權限判斷、下拉選單與各處的關聯都靠它——這種情況拆成兩列，
        // 因此同一個 DaAssetCode 會出現不只一次。

        // SQLite 沒有原生 rowversion，改以 Guid 當並行權杖，於 SaveChanges 換新值
        modelBuilder.Entity<InfoSystem>().Property(e => e.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<DataAsset>().Property(e => e.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<SystemInventory>().Property(e => e.RowVersion).IsConcurrencyToken();
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
        ApplyFixedAndMultiValueFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        RecalculateAssetValues();
        ApplyFixedAndMultiValueFields();
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
    /// 兩件存檔前的整理，放在這裡是因為新增與編輯兩條路徑都會經過，
    /// 集中處理才不會有某一條忘了做。
    ///
    /// 一、保管單位與風險擁有者是業務端指定的固定值。畫面上已經是唯讀，
    ///     但唯讀只是操作防呆，改個表單欄位就繞過去了，所以這裡再蓋一次。
    /// 二、複選欄位的分隔符統一成「/」。來源試算表混用了 / ; ;# 三種，
    ///     不統一的話同一組答案會有好幾種寫法，比對與統計都會漏。
    /// </summary>
    private void ApplyFixedAndMultiValueFields()
    {
        foreach (var entry in ChangeTracker.Entries<InfoSystem>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            var system = entry.Entity;
            system.SwCustodianUnit = FieldOptions.FixedCustodianUnit;
            system.SwRiskOwner = FieldOptions.FixedRiskOwner;

            system.SwDevMode = MultiValue.Normalize(system.SwDevMode);
            system.SwMaintMode = MultiValue.Normalize(system.SwMaintMode);
            system.SwAppMaintainerDeputy = MultiValue.Normalize(system.SwAppMaintainerDeputy);
            system.SwLocation = MultiValue.Normalize(system.SwLocation);
            system.SwLanguage = MultiValue.Normalize(system.SwLanguage);
        }

        foreach (var entry in ChangeTracker.Entries<DataAsset>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            entry.Entity.DaHasSensitiveData = MultiValue.Normalize(entry.Entity.DaHasSensitiveData);
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

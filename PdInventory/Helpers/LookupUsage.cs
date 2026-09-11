using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>維護資料的種類。清單的「N 筆」與明細視窗都靠它決定要掃哪些欄位。</summary>
public enum UsageKind
{
    /// <summary>共用：部門。</summary>
    Department,
    /// <summary>共用：人員。</summary>
    Employee,
    /// <summary>共用：資管維運。</summary>
    OpsStaff,
    /// <summary>各表單的欄位選項，另須指定是哪一個欄位。</summary>
    FieldOption,
    /// <summary>3-1 風險分類編號。</summary>
    RiskCategory,
    /// <summary>3-2 評估影響程度。</summary>
    RiskImpact,
    /// <summary>3-3 評估發生可能性。</summary>
    RiskLikelihood,
    /// <summary>3-4 有效性評估。</summary>
    RiskEffectiveness,
    /// <summary>附表一：個人資料類別。</summary>
    PdCategory,
    /// <summary>附表二：特定目的列表。</summary>
    Purpose,
}

/// <summary>
/// 「這筆維護資料還被哪些資料使用」。
///
/// 為什麼每張維護表都要有：各表單存的是**文字**而不是外鍵，資料庫不會擋刪除。
/// 刪掉一個還在用的值，引用它的那些資料就變成查不到來源的孤兒值，
/// 而且畫面上不會有任何警告——這正是風險自評那四張維護表原本的狀況。
///
/// 這裡回傳的是**明細列**，清單上的「N 筆」由 <c>rows.Count</c> 得出。
/// 兩者若各寫一套查詢遲早會不一致——徽章寫 3 筆、點開只有 2 筆，
/// 而且沒有任何東西會報錯。
///
/// 多選欄位（以 / 分隔）不能用等號比對，一律走 <see cref="MultiValue.Contains"/>。
/// </summary>
public static class LookupUsage
{
    /// <summary>
    /// 某一種維護資料的每個值各被哪些資料使用。回傳的字典一定涵蓋該維護表的所有值，
    /// 沒有人用的就是空清單——畫面才分得出「沒人用」與「查不到這個值」。
    /// </summary>
    /// <param name="field">
    /// <see cref="UsageKind.FieldOption"/> 專用：<see cref="OptionCatalog"/> 登記的屬性名稱。
    /// </param>
    public static async Task<Dictionary<string, List<UsageRow>>> RowsAsync(
        AppDbContext db, UsageKind kind, string? field = null)
    {
        var scan = await Snapshot.LoadAsync(db, kind, field);

        return kind switch
        {
            UsageKind.Department => scan.Collect(
                await db.Departments.Select(d => d.Name).ToListAsync(),
                [
                    scan.Software(nameof(InfoSystem.SwBusinessOwnerUnit), s => s.SwBusinessOwnerUnit),
                    scan.Software(nameof(InfoSystem.SwUserAccountGrant), s => s.SwUserAccountGrant),
                    scan.Software(nameof(InfoSystem.SwUserUnit), s => s.SwUserUnit),
                    scan.Data(nameof(DataAsset.DaUserUnit), d => d.DaUserUnit),
                    scan.Transfers(nameof(TransferRecord.InternalUnit), t => t.InternalUnit),
                ]),

            UsageKind.Employee => scan.Collect(
                await db.Employees.Select(e => e.Name).ToListAsync(),
                [
                    scan.Software(nameof(InfoSystem.SwAppManager), s => s.SwAppManager),
                    scan.Software(nameof(InfoSystem.SwAppMaintainer), s => s.SwAppMaintainer),
                    scan.Software(nameof(InfoSystem.SwAppMaintainerDeputy), s => s.SwAppMaintainerDeputy),
                    scan.Software(nameof(InfoSystem.SwDeveloper), s => s.SwDeveloper),
                    scan.Software(nameof(InfoSystem.SwReviewer), s => s.SwReviewer),
                    scan.Data(nameof(DataAsset.DaReviewer), d => d.DaReviewer),
                ]),

            UsageKind.OpsStaff => scan.Collect(
                await db.OpsStaffs.Select(o => o.Name).ToListAsync(),
                [
                    scan.Software(nameof(InfoSystem.SwOperator), s => s.SwOperator),
                    scan.Software(nameof(InfoSystem.SwDeployer), s => s.SwDeployer),
                ]),

            UsageKind.FieldOption => scan.Collect(
                await db.FieldOptionItems.Where(o => o.FieldName == field)
                                         .Select(o => o.Value).ToListAsync(),
                [scan.FieldOption(field!)]),

            // 風險自評存的是分類代號
            UsageKind.RiskCategory => scan.Collect(
                await db.RiskCategories.Select(r => r.Code).ToListAsync(),
                [scan.Risk(nameof(InventoryItem.RiskCategoryCode), i => i.RiskCategoryCode)]),

            UsageKind.RiskImpact => scan.RiskLevels(
                await db.RiskImpactLevels.Select(l => new Level(l.Level, l.Name)).ToListAsync(),
                nameof(InventoryItem.RiskImpactLevel), i => i.RiskImpactLevel),

            UsageKind.RiskLikelihood => scan.RiskLevels(
                await db.RiskLikelihoodLevels.Select(l => new Level(l.Level, l.Name)).ToListAsync(),
                nameof(InventoryItem.RiskLikelihoodLevel), i => i.RiskLikelihoodLevel),

            UsageKind.RiskEffectiveness => scan.RiskLevels(
                await db.RiskEffectivenessLevels.Select(l => new Level(l.Level, l.Name)).ToListAsync(),
                nameof(InventoryItem.RiskEffectivenessLevel), i => i.RiskEffectivenessLevel),

            // 附表一與附表二是真正的多對多（有中介表），不是文字比對，直接走導覽屬性。
            // 不在 SQL 裡攤平：EF 會把「多對多 ＋ 全域查詢篩選」翻成需要 APPLY 的查詢，
            // 而 SQLite 不支援 APPLY，執行期會直接擲例外。
            UsageKind.PdCategory => scan.Linked(
                await db.Categories.Select(c => c.Code).ToListAsync(),
                i => i.Categories.Select(c => c.Code),
                "使用資料(欄位)"),

            UsageKind.Purpose => scan.Linked(
                await db.Purposes.Select(p => p.Code).ToListAsync(),
                i => i.Purposes.Select(p => p.Code),
                "特定目的"),

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知的維護資料種類"),
        };
    }

    /// <summary>清單上的「N 筆」。與明細出自同一份資料，不可能對不上。</summary>
    public static async Task<Dictionary<string, int>> CountsAsync(
        AppDbContext db, UsageKind kind, string? field = null) =>
        (await RowsAsync(db, kind, field)).ToDictionary(p => p.Key, p => p.Value.Count);

    /// <summary>某一個值被幾筆資料使用。刪除前的檢查用。</summary>
    public static async Task<int> CountAsync(
        AppDbContext db, UsageKind kind, string value, string? field = null) =>
        (await RowsAsync(db, kind, field)).GetValueOrDefault(value)?.Count ?? 0;

    /// <summary>等級的維護資料。Label 在實體上是算出來的屬性，EF 翻不成 SQL，因此撈回來再組。</summary>
    private sealed record Level(int Number, string Name)
    {
        /// <summary>畫面上存進欄位的字串，例如「2：中等」。寫法與實體上的 Label 一致。</summary>
        public string Label => $"{Number}：{Name}";
    }

    /// <summary>
    /// 掃描用的資料快照。各表都只有幾十筆，一次撈回記憶體再比對最直接；
    /// 多選欄位本來就不可能在 SQL 裡比對。
    /// </summary>
    private sealed class Snapshot
    {
        private List<InfoSystem> _systems = [];
        private List<DataAsset> _dataAssets = [];
        private List<SystemInventory> _inventories = [];
        private List<InventoryItem> _items = [];
        private List<TransferRecord> _transfers = [];

        /// <summary>SW 與盤點表的檢視畫面都掛在 DA 底下，要靠資產編號換主鍵。</summary>
        private Dictionary<string, int> _dataAssetIds = [];

        /// <summary>盤點表沒有資產名稱，顯示時要從 SW 補。</summary>
        private Dictionary<string, string> _systemNames = [];

        public static async Task<Snapshot> LoadAsync(AppDbContext db, UsageKind kind, string? field)
        {
            var scan = new Snapshot();

            // 只載真的要掃的表：風險自評與兩張附表都只碰個資盤點表那一張
            var needsAssets = kind is UsageKind.Department or UsageKind.Employee
                              or UsageKind.OpsStaff or UsageKind.FieldOption;

            if (needsAssets)
            {
                scan._systems = await db.InfoSystems.ToListAsync();
                scan._dataAssets = await db.DataAssets.ToListAsync();
                scan._inventories = await db.SystemInventories.ToListAsync();
                scan._dataAssetIds = scan._dataAssets
                    .Where(d => !string.IsNullOrWhiteSpace(d.SystemCode))
                    .ToDictionary(d => d.SystemCode, d => d.Id);
                scan._systemNames = scan._systems
                    .Where(s => !string.IsNullOrWhiteSpace(s.SystemCode))
                    .ToDictionary(s => s.SystemCode, s => s.SystemName);
            }

            if (kind is UsageKind.Department)
                scan._transfers = await db.TransferRecords.ToListAsync();

            // 個資盤點表與風險自評同為 InventoryItems；欄位選項也可能落在這張表
            if (!needsAssets || OwnerOf(field) == typeof(InventoryItem))
            {
                var items = db.InventoryItems.AsQueryable();
                if (kind is UsageKind.PdCategory) items = items.Include(i => i.Categories);
                if (kind is UsageKind.Purpose) items = items.Include(i => i.Purposes);
                scan._items = await items.ToListAsync();
            }

            return scan;
        }

        /// <summary>一個要掃的欄位：每一筆資料的那一格值，以及命中時要產出的明細列。</summary>
        public delegate IEnumerable<(string Value, UsageRow Row)> Source();

        /// <summary>把所有來源掃過一遍，依維護表的每個值分堆。</summary>
        public Dictionary<string, List<UsageRow>> Collect(IEnumerable<string> keys, Source[] sources)
        {
            var result = keys.Distinct().ToDictionary(k => k, _ => new List<UsageRow>());
            var lookup = result.Keys.ToList();

            foreach (var source in sources)
            {
                foreach (var (value, row) in source())
                {
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    foreach (var key in lookup)
                    {
                        if (MultiValue.Contains(value, key)) result[key].Add(row);
                    }
                }
            }

            return result;
        }

        public Source Software(string property, Func<InfoSystem, string> cell)
        {
            var label = Label<InfoSystem>(property);
            return () => _systems.Select(s => (cell(s), new UsageRow(
                UsageLists.Software,
                _dataAssetIds.GetValueOrDefault(s.SystemCode),
                [s.SystemCode, s.SystemName],
                label)));
        }

        public Source Data(string property, Func<DataAsset, string> cell)
        {
            var label = Label<DataAsset>(property);
            return () => _dataAssets.Select(d => (cell(d), new UsageRow(
                UsageLists.Data, d.Id, [d.SystemCode, d.DaAssetCode], label)));
        }

        /// <summary>
        /// 盤點表目前沒有任何欄位吃維護資料，因此這條路不會有資料走過。
        /// 留著是因為它與另外五張清單是同一組設定，之後盤點表要加下拉時
        /// 只要在上面的 switch 多一行，不必回來補這裡。
        /// </summary>
        public Source Systems(string property, Func<SystemInventory, string> cell)
        {
            var label = Label<SystemInventory>(property);
            return () => _inventories.Select(i => (cell(i), new UsageRow(
                UsageLists.Systems,
                _dataAssetIds.GetValueOrDefault(i.SystemCode),
                [i.SystemCode, _systemNames.GetValueOrDefault(i.SystemCode, "")],
                label)));
        }

        public Source Inventory(string property, Func<InventoryItem, string> cell)
        {
            var label = Label<InventoryItem>(property);
            return () => _items.Select(i => (cell(i), InventoryRow(i, label)));
        }

        public Source Transfers(string property, Func<TransferRecord, string> cell)
        {
            var label = Label<TransferRecord>(property);
            return () => _transfers.Select(t => (cell(t), new UsageRow(
                UsageLists.Transfers, t.Id,
                [t.SystemCode, t.SystemName, t.TransferType], label)));
        }

        public Source Risk(string property, Func<InventoryItem, string> cell)
        {
            var label = Label<InventoryItem>(property);
            return () => _items.Select(i => (cell(i), RiskRow(i, label)));
        }

        private static UsageRow InventoryRow(InventoryItem i, string label) => new(
            UsageLists.Inventory, i.Id, [i.SystemCode, i.SystemName, i.DocumentName], label);

        private static UsageRow RiskRow(InventoryItem i, string label) => new(
            UsageLists.Risk, i.Id, [i.SystemCode, i.SystemName, i.DocumentName], label);

        /// <summary>
        /// 欄位選項：屬性掛在哪個實體上就掃哪一張表。登記在這三張以外的實體會直接擲出例外
        /// ——寧可當場壞掉，也不要安靜地算成 0 而讓刪除保護失效。
        /// </summary>
        public Source FieldOption(string field)
        {
            var owner = OwnerOf(field);

            if (owner == typeof(InfoSystem)) return Software(field, s => Text(s, field));
            if (owner == typeof(DataAsset)) return Data(field, d => Text(d, field));
            if (owner == typeof(InventoryItem)) return Inventory(field, i => Text(i, field));

            throw new InvalidOperationException(
                $"OptionCatalog 登記的欄位 {field} 不屬於 InfoSystem／DataAsset／InventoryItem，"
                + "請在 LookupUsage 補上對應的資料表。");
        }

        /// <summary>
        /// 3-2~3-4 的等級。畫面上選的是「2：中等」這種字串，但既有資料也有只存「2」的
        /// （有效性評估那一欄就是），兩種寫法都要算成使用中，
        /// 否則會誤判成沒人用而放行刪除。
        /// </summary>
        public Dictionary<string, List<UsageRow>> RiskLevels(
            List<Level> levels, string property, Func<InventoryItem, string> cell)
        {
            var label = Label<InventoryItem>(property);
            var result = levels.ToDictionary(l => l.Number.ToString(), _ => new List<UsageRow>());

            foreach (var item in _items)
            {
                var value = cell(item);
                foreach (var level in levels)
                {
                    if (value == level.Label || value == level.Number.ToString())
                        result[level.Number.ToString()].Add(RiskRow(item, label));
                }
            }

            return result;
        }

        /// <summary>附表一與附表二：關聯由導覽屬性給，照著分堆即可。</summary>
        public Dictionary<string, List<UsageRow>> Linked(
            List<string> keys, Func<InventoryItem, IEnumerable<string>> codesOf, string label)
        {
            var result = keys.ToDictionary(k => k, _ => new List<UsageRow>());

            foreach (var item in _items)
            {
                foreach (var code in codesOf(item))
                {
                    if (result.TryGetValue(code, out var rows)) rows.Add(InventoryRow(item, label));
                }
            }

            return result;
        }

        private static string Text(object entity, string property) =>
            entity.GetType().GetProperty(property)!.GetValue(entity) as string ?? "";
    }

    /// <summary>哪個實體有這個屬性。欄位選項靠它決定要掃哪一張表。</summary>
    private static Type? OwnerOf(string? field)
    {
        if (string.IsNullOrEmpty(field)) return null;
        if (typeof(InfoSystem).GetProperty(field) is not null) return typeof(InfoSystem);
        if (typeof(DataAsset).GetProperty(field) is not null) return typeof(DataAsset);
        if (typeof(InventoryItem).GetProperty(field) is not null) return typeof(InventoryItem);
        return null;
    }

    /// <summary>欄位標題取實體上的 [Display]，名稱只在實體宣告一次。</summary>
    private static string Label<T>(string property) =>
        typeof(T).GetProperty(property)?.GetCustomAttribute<DisplayAttribute>()?.Name ?? property;
}

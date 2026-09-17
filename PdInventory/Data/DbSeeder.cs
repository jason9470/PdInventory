using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PdInventory.Models;

namespace PdInventory.Data;

/// <summary>首次啟動時，將 個資清冊.xlsx 匯出的 JSON 種子資料匯入資料庫。</summary>
public static class DbSeeder
{
    private record InventorySeed(
        string SeqNo, string DocumentName, string SubjectCount,
        List<string> CategoryCodes, string SubjectType,
        bool HasSpecialData, string SpecialDataLegalBasis, bool MinFieldCompliant,
        List<string> PurposeCodes,
        string SystemCode, string SystemName, string SourceCode, string SourceName, string CompanyRole,
        string CollectProcedure, string CollectStatement, string CollectConsent,
        string ProcessProcedure, string ProcessDept, string ProcessStatement, string ProcessConsent,
        string TransferTarget, string TransferContract, string TransferMethod, string TransferCountry,
        string RetentionPaper, string RetentionDigital, string LocationPaper, string LocationDigital,
        string Disposal, string Remark);

    public static void Seed(AppDbContext db, string contentRootPath)
    {
        // 以 Migrations 建立／升級結構：資料庫不存在時建出全部資料表，
        // 既有資料庫則只套用尚未執行的 Migration。
        db.Database.Migrate();

        var seedDir = Path.Combine(contentRootPath, "Data", "Seed");
        if (!Directory.Exists(seedDir)) return;

        T? Load<T>(string file)
        {
            var path = Path.Combine(seedDir, file);
            if (!File.Exists(path)) return default;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }

        if (!db.Categories.Any())
        {
            var cats = Load<List<PdCategory>>("categories.json");
            if (cats is not null) db.Categories.AddRange(cats);
            db.SaveChanges();
        }

        if (!db.Purposes.Any())
        {
            var purs = Load<List<Purpose>>("purposes.json");
            if (purs is not null) db.Purposes.AddRange(purs);
            db.SaveChanges();
        }

        if (!db.InventoryItems.Any())
        {
            var items = Load<List<InventorySeed>>("inventory.json");
            if (items is not null)
            {
                var catByCode = db.Categories.ToDictionary(c => c.Code);
                var purByCode = db.Purposes.ToDictionary(p => p.Code);
                foreach (var s in items)
                {
                    var item = new InventoryItem
                    {
                        SeqNo = s.SeqNo, DocumentName = s.DocumentName, SubjectCount = s.SubjectCount,
                        SubjectType = s.SubjectType, HasSpecialData = s.HasSpecialData,
                        SpecialDataLegalBasis = s.SpecialDataLegalBasis, MinFieldCompliant = s.MinFieldCompliant,
                        SystemCode = s.SystemCode, SystemName = s.SystemName,
                        SourceCode = s.SourceCode, SourceName = s.SourceName, CompanyRole = s.CompanyRole,
                        CollectProcedure = s.CollectProcedure, CollectStatement = s.CollectStatement,
                        CollectConsent = s.CollectConsent,
                        ProcessProcedure = s.ProcessProcedure, ProcessDept = s.ProcessDept,
                        ProcessStatement = s.ProcessStatement, ProcessConsent = s.ProcessConsent,
                        TransferTarget = s.TransferTarget, TransferContract = s.TransferContract,
                        TransferMethod = s.TransferMethod, TransferCountry = s.TransferCountry,
                        RetentionPaper = s.RetentionPaper, RetentionDigital = s.RetentionDigital,
                        LocationPaper = s.LocationPaper, LocationDigital = s.LocationDigital,
                        Disposal = s.Disposal, Remark = s.Remark,
                    };
                    item.Categories.AddRange(s.CategoryCodes
                        .Where(catByCode.ContainsKey).Select(c => catByCode[c]));
                    item.Purposes.AddRange(s.PurposeCodes
                        .Where(purByCode.ContainsKey).Select(c => purByCode[c]));
                    db.InventoryItems.Add(item);
                }
                db.SaveChanges();
            }
        }

        if (!db.TransferRecords.Any())
        {
            var rows = Load<List<TransferRecord>>("transfers.json");
            if (rows is not null) db.TransferRecords.AddRange(rows);
            db.SaveChanges();
        }

        if (!db.InfoSystems.Any())
        {
            var rows = Load<List<InfoSystem>>("systems.json");
            if (rows is not null) db.InfoSystems.AddRange(rows);
            db.SaveChanges();
        }

        if (!db.Departments.Any())
        {
            var depts = Load<List<Department>>("departments.json");
            if (depts is not null) db.Departments.AddRange(depts);
            db.SaveChanges();
        }

        if (!db.OpsStaffs.Any())
        {
            var staff = Load<List<OpsStaff>>("opsstaff.json");
            if (staff is not null) db.OpsStaffs.AddRange(staff);
            db.SaveChanges();
        }

        if (!db.Employees.Any())
        {
            var employees = Load<List<Employee>>("employees.json");
            if (employees is not null) db.Employees.AddRange(employees);
            db.SaveChanges();
        }

        SeedRiskLookups(db);
        SeedUsers(db);
        SeedUsersForEmployees(db);
    }

    /// <summary>
    /// 初始使用者。只在 AppUsers 完全空白時建立一次，之後的異動一律以畫面操作為準，
    /// 重新啟動不會把管理者刪掉的測試帳號救回來。
    ///
    /// 正常情況下使用者不必在這裡建：不是第一次登入時自動建檔，就是管理者從人員表新增時
    /// 一併建立（見 Helpers/UserProvisioning.cs）。這裡只解決一個開機問題——
    /// 要先有一個管理者，否則沒有人進得了權限設定畫面。
    /// </summary>
    private static void SeedUsers(AppDbContext db)
    {
        if (db.AppUsers.Any()) return;

        db.AppUsers.Add(new AppUser
        {
            EmpNo = "0183253",
            EmpName = "林子耕",
            Role = UserRole.Admin,
        });

        db.SaveChanges();
    }

    /// <summary>
    /// 人員表裡的每個人都要在權限設定看得到，否則管理者無從指定他的角色。
    ///
    /// 平常這件事由 <see cref="Helpers.UserProvisioning.EnsureUserForEmployeeAsync"/> 處理
    /// （管理者從人員表建檔時一併建立帳號），但名冊是直接匯入資料表的，沒有經過那條路，
    /// 所以這裡補一次。已經有帳號的不動——角色是設定過的，不能覆蓋。
    ///
    /// 沒有員工編號的略過：兩張表以員編相認，沒有編號就無從對應登入身分。
    /// </summary>
    private static void SeedUsersForEmployees(AppDbContext db)
    {
        var existing = db.AppUsers.IgnoreQueryFilters()
            .Select(u => u.EmpNo)
            .ToHashSet();

        var missing = db.Employees
            .Where(e => e.EmpNo != "")
            .Where(e => !existing.Contains(e.EmpNo))
            .Select(e => new { e.EmpNo, e.Name })
            .ToList();

        if (missing.Count == 0) return;

        db.AppUsers.AddRange(missing.Select(e => new AppUser
        {
            EmpNo = e.EmpNo,
            EmpName = e.Name,
            Role = UserRole.AssetOwner,
        }));
        db.SaveChanges();
    }

    /// <summary>3-1~3-4：風險自評下拉維護資料（固定參考表，內建種子）。</summary>
    private static void SeedRiskLookups(AppDbContext db)
    {
        if (!db.RiskCategories.Any())
        {
            db.RiskCategories.AddRange(
                new RiskCategory { Code = "RIPP001", CategoryName = "個人資料風險 - 內部處理(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]未有適當之標示，致使人員疏忽，發生誤刪、誤讀、遺失等情況。", ControlReference = "- 個資盤點 (個資形式清單)\n- 個資標示" },
                new RiskCategory { Code = "RIPP002", CategoryName = "個人資料風險 - 內部處理(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]於內部處理過程中，未有適當之控管，致使個人資料外洩/遺失。", ControlReference = "-實體權限\n-處理環境的控管(如櫃台)\n-人員認知\n-桌面淨空\n-列印、影印的管理" },
                new RiskCategory { Code = "RIPP003", CategoryName = "個人資料風險 - 內部處理(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]於輸入系統及內部處理過程中或輸入之前(資料蒐集階段)，未有適當的覆核及檢查機制，致使個人資料的正確性受到損害。", ControlReference = "-輸入的檢核機制(系統/人工)\n-蒐集時資料核對與確認\n-使用時的確認(人員認知)\n-資料的即時更新機制(由外部發動)" },
                new RiskCategory { Code = "RIPP004", CategoryName = "個人資料風險 - 內部處理(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]未有適當的存取控管或覆核存取狀況之機制，致使人員故意將個人資料外洩。", ControlReference = "-存取權限的核准(符合最小授與原則)\n-存取權限的覆核\n-存取紀錄的覆核" },
                new RiskCategory { Code = "RIPE001", CategoryName = "個人資料風險 - 內部處理(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]之電腦未有適當的監控與防護措施，致使個人資料由個人電腦的管道外洩。", ControlReference = "-防毒、防駭\n-實體隔離\n-使用狀況監控\n-軟體安裝管理(防止P2P)" },
                new RiskCategory { Code = "RIPE002", CategoryName = "個人資料風險 - 內部處理(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]之電腦對外連線及傳輸資料管道(如：燒錄機、USB埠、電子郵件或網頁等)未有適當之控管措施，致使資料外洩。", ControlReference = "-USB埠的控管\n-電子郵件\n-網頁的限制" },
                new RiskCategory { Code = "RIPE003", CategoryName = "個人資料風險 - 內部處理(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]之可攜式設備(如：筆記型電腦、光碟片或可攜式儲存媒體等)未有適當之控管措施，致使資料外洩/遺失/滅失。", ControlReference = "-存放資料的加密\n-密碼保護機制\n-設備的管控(可否攜帶回家)\n-媒體的保存(光碟片、磁帶等)" },
                new RiskCategory { Code = "RIPE004", CategoryName = "個人資料風險 - 內部處理(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]過程中，因[資訊系統名稱]設計不當，致使人員疏忽，發生誤刪、誤讀、遺失等情況。", ControlReference = "-系統防呆機制\n-系統權限的授與\n-人員操作訓練" },
                new RiskCategory { Code = "RITP001", CategoryName = "個人資料風險 - 內部傳遞(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]於傳遞過程(含傳真及實體遞送)中，未適當考量保護措施，致使個資遺失/外洩", ControlReference = "-傳遞時彌封\n-簽收\n-傳遞人員的考量\n-傳真機的控管" },
                new RiskCategory { Code = "RITE001", CategoryName = "個人資料風險 - 內部傳遞(系統/電子檔)", EventDescription = "[個人資料文件/檔案名稱]使用數位形式傳輸/交換時，途中遭到竊取(中間人攻擊)，致使個資外洩。", ControlReference = "-考量網路區隔狀況\n-傳輸環境的安全性(LAN或WLAN)\n-檔案以密碼保護\n-可額外電話線路的防護性" },
                new RiskCategory { Code = "RITE002", CategoryName = "個人資料風險 - 內部傳遞(系統/電子檔)", EventDescription = "存放[個人資料文件/檔案名稱]之媒體於傳遞過程中，未有安全防護，致使資料外洩", ControlReference = "-加密存放(密碼及資料分送)\n-傳遞時彌封\n-簽收\n-傳遞人員的考量" },
                new RiskCategory { Code = "RITE003", CategoryName = "個人資料風險 - 內部傳遞(系統/電子檔)", EventDescription = "內部人員於處理/利用[個人資料文件/檔案名稱]時誤將電子檔案刪除或人員誤送資料(人為操作錯誤)，導致個資毀損或遺失", ControlReference = "-系統防呆\n-備份機制\n-人員認知" },
                new RiskCategory { Code = "RETP001", CategoryName = "個人資料風險 - 外部傳遞(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]於傳遞予[外部單位名稱]之過程(含傳真及實體遞送)中，未適當考量保護措施，致使個資遺失/外洩", ControlReference = "-傳遞時彌封\n-簽收\n-傳遞人員的考量\n-傳真機的控管(傳遞出去後立刻打電話去控管、收到傳真之後，要立刻取走)" },
                new RiskCategory { Code = "RETE001", CategoryName = "個人資料風險 - 外部傳遞(系統/電子檔)", EventDescription = "將[個人資料文件/檔案名稱]以數位形式傳遞予[外部單位名稱]時，未適當考量保護措施，致使個資遺失/外洩", ControlReference = "-傳輸通道的加密(VPN)\n-傳輸資料的加密(SSL)\n-檔案以密碼保護" },
                new RiskCategory { Code = "R0SP001", CategoryName = "個人資料風險 - 保存(紙本/書面文件)", EventDescription = "個人資料文件之存放地點未適當保護，致使個資外洩/遺失", ControlReference = "-存放位置的實體防護措施\n-進出的控管(含盤點)" },
                new RiskCategory { Code = "R0SP002", CategoryName = "個人資料風險 - 保存(紙本/書面文件)", EventDescription = "個人資料文件之存放地點未適當考量環境風險，致使資料毀損", ControlReference = "-消防\n-溫溼度控管\n-防災" },
                new RiskCategory { Code = "R0SE001", CategoryName = "個人資料風險 - 保存(系統/電子檔)", EventDescription = "已逾保存時限之[個人資料文件/檔案名稱]，未及時刪除，致使個人資料檔案從個人電腦外洩/遺失的機率提高。", ControlReference = "-定期清查\n-使用完畢或使用/保存期限超過即刪除" },
                new RiskCategory { Code = "R0SE002", CategoryName = "個人資料風險 - 保存(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]之可攜式設備(如：筆記型電腦、光碟片或可攜式儲存媒體等)的保存未有適當之控管措施，致使資料外洩/遺失/滅失。", ControlReference = "-存放資料的加密\n-存放位置的實體防護措施\n-進出的控管(含盤點)\n-密碼保護機制\n-設備的管控\n-媒體的保存(光碟片、磁帶等)\n-消防\n-溫濕度控管\n-防災\n-備份" },
                new RiskCategory { Code = "R0DP001", CategoryName = "個人資料風險 - 銷毀(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]銷毀方式不佳，致使惡意人員藉由拼湊方式，恢復文件內容，導致個資外洩", ControlReference = "-銷毀機制(水銷\\火銷等)" },
                new RiskCategory { Code = "R0DP002", CategoryName = "個人資料風險 - 銷毀(紙本/書面文件)", EventDescription = "[個人資料文件/檔案名稱]於銷毀過程時，未有適當的控管，導致個人資料於銷毀的過程中外洩", ControlReference = "-銷毀要有紀錄和授權\n-填寫錯誤文件的銷毀" },
                new RiskCategory { Code = "R0DE001", CategoryName = "個人資料風險 - 銷毀(系統/電子檔)", EventDescription = "處理[個人資料文件/檔案名稱]之可攜式設備(如：筆記型電腦、光碟片或可攜式儲存媒體等)的銷毀未有適當之控管措施，致使資料外洩/遺失/滅失。", ControlReference = "-銷毀機制(水銷\\火銷等)" },
                new RiskCategory { Code = "R0O0001", CategoryName = "個人資料風險 - 委外", EventDescription = "與[公司名稱]之委外合約簽訂時，未清楚說明雙方於個人資料使用之權利與義務，致使發生損害事件時，無法釐清責任", ControlReference = "-委外合約條文包含雙方之權利義務" },
                new RiskCategory { Code = "R0O0002", CategoryName = "個人資料風險 - 委外", EventDescription = "未善盡對[公司名稱]委外管理責任，致使委外處理之個人資料發生損害", ControlReference = "-委外合約應有明確要求處理個人資料之基本控管水準\n-委外監控管理機制" },
                new RiskCategory { Code = "R0E0001", CategoryName = "個人資料風險 - 個人資料事件發生時的損害處理", EventDescription = "--", ControlReference = "-稽核紀錄管理(包含同意、操作、銷毀、調閱個人資料文件)\n-事件通報機制\n-通知當事人\n-事件抱怨處理機制" });
            db.SaveChanges();
        }

        if (!db.RiskImpactLevels.Any())
        {
            db.RiskImpactLevels.AddRange(
                new RiskImpactLevel { Level = 1, Name = "輕微", FinancialImpact = "對財務幾乎沒有負面影響", ReputationImpact = "‧ 單一地方媒體負面報導\n‧ 來自單一客戶、團體或非政府組織之抱怨", PrivacyImpact = "洩漏資訊，對個資當事人無影響" },
                new RiskImpactLevel { Level = 2, Name = "中等", FinancialImpact = "對財務有負面的影響，但影響程度輕微", ReputationImpact = "‧ 多則負面新聞報導。\n‧ 單一風險事件之抱怨明顯增加", PrivacyImpact = "洩漏資訊，對個資當事人不太有影響" },
                new RiskImpactLevel { Level = 3, Name = "顯著", FinancialImpact = "對財務有負面的影響，需要關注及管理", ReputationImpact = "‧ 全國性媒體負面報導\n‧ 來自眾多客戶、團體或非政府組織之抱怨", PrivacyImpact = "洩漏資訊，對個資當事人產生影響" },
                new RiskImpactLevel { Level = 4, Name = "嚴重", FinancialImpact = "對財務有嚴重的影響，需要密切關注並立即執行改善方案", ReputationImpact = "‧ 多則且持續一段時間之負面新聞報導\n‧ 大規模之客戶抱怨", PrivacyImpact = "洩漏資訊，對個資當事人有重大影響" },
                new RiskImpactLevel { Level = 5, Name = "非常嚴重", FinancialImpact = "對財務有極為嚴重的影響，除需要立即執行改善方案外，可能還需要外部單位的協助", ReputationImpact = "‧ 國際性媒體之負面報導\n‧ 已發展成嚴重之社會議題\n‧ 主管機關公開宣布之不當行為", PrivacyImpact = "洩漏資訊，對個資當事人產生生命財產之危害" });
            db.SaveChanges();
        }

        if (!db.RiskLikelihoodLevels.Any())
        {
            db.RiskLikelihoodLevels.AddRange(
                new RiskLikelihoodLevel { Level = 1, Name = "不太可能發生", Situation = "損失事件幾乎不會發生", Frequency = "1.超過5年才可能發生\n2.或僅於某些特殊情況下發生" },
                new RiskLikelihoodLevel { Level = 2, Name = "極少發生", Situation = "損失事件可能會發生，但機率很低", Frequency = "1.每2~5年至少發生1次\n2.或可能會發生但不常見" },
                new RiskLikelihoodLevel { Level = 3, Name = "可能發生", Situation = "損失事件會發生，一般的發生機率", Frequency = "1.每2年至少發生1次\n2.或某些時候可能會發生" },
                new RiskLikelihoodLevel { Level = 4, Name = "經常發生", Situation = "損失事件會發生，且發生機率高", Frequency = "1.每年至少發生1次\n2.或多數情況下應該會發生" },
                new RiskLikelihoodLevel { Level = 5, Name = "定期重複發生", Situation = "損失事件幾乎確定會發生", Frequency = "1.每年至少發生2次(含) 以上\n2.或會發生機率非常高" });
            db.SaveChanges();
        }

        if (!db.RiskEffectivenessLevels.Any())
        {
            db.RiskEffectivenessLevels.AddRange(
                new RiskEffectivenessLevel { Level = 1, Name = "非常良好", Effectiveness = "80~100%", CheckResult = "有明確管理規範、程序章程\n落實執行控制措施" },
                new RiskEffectivenessLevel { Level = 2, Name = "良好", Effectiveness = "60~80%", CheckResult = "有明確管理規範、程序章程\n執行控制措施可再加強" },
                new RiskEffectivenessLevel { Level = 3, Name = "可接受", Effectiveness = "40~60%", CheckResult = "未有管理規範、程序章程\n執行控制措施可再加強" },
                new RiskEffectivenessLevel { Level = 4, Name = "欠佳", Effectiveness = "20~40%", CheckResult = "未有管理規範、程序章程\n執行控制措施執行欠佳" },
                new RiskEffectivenessLevel { Level = 5, Name = "不良", Effectiveness = "0~20%", CheckResult = "未有管理規範、程序章程\n執行控制措施執行不佳良" });
            db.SaveChanges();
        }
    }
}

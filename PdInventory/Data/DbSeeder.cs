using System.Text.Json;
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
        string SystemName, string SourceName, string CompanyRole,
        string CollectProcedure, string CollectStatement, string CollectConsent,
        string ProcessProcedure, string ProcessDept, string ProcessStatement, string ProcessConsent,
        string TransferTarget, string TransferContract, string TransferMethod, string TransferCountry,
        string RetentionPaper, string RetentionDigital, string LocationPaper, string LocationDigital,
        string Disposal, string Remark);

    public static void Seed(AppDbContext db, string contentRootPath)
    {
        db.Database.EnsureCreated();

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
                        SystemName = s.SystemName, SourceName = s.SourceName, CompanyRole = s.CompanyRole,
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
    }
}

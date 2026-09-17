using System.Collections;
using System.Reflection;
using ClosedXML.Excel;
using PdInventory.Data;
using PdInventory.Models;

namespace PdInventory.Helpers;

/// <summary>依「維護匯出」設定的欄位順序，把清單資料寫成 .xlsx。</summary>
public static class ExcelExporter
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static async Task<(byte[] Content, string FileName)> BuildAsync(
        AppDbContext db, string listKey, IEnumerable<object> rows)
    {
        var columns = (await ExportCatalog.ResolveAsync(db, listKey))
            .Where(c => c.Included)
            .ToList();

        var entity = ExportCatalog.Lists[listKey].Entity;
        var props = columns
            .Select(c => entity.GetProperty(c.PropertyName))
            .Where(p => p is not null)
            .Cast<PropertyInfo>()
            .ToList();

        using var workbook = new XLWorkbook();
        // 工作表名稱不得含 : \ / ? * [ ]，且上限 31 字元
        var sheetName = SafeSheetName(ExportCatalog.Lists[listKey].Title);
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < columns.Count; i++)
            sheet.Cell(1, i + 1).Value = columns[i].Title;

        var header = sheet.Row(1);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        var rowIndex = 2;
        foreach (var item in rows)
        {
            for (var i = 0; i < props.Count; i++)
                sheet.Cell(rowIndex, i + 1).SetValue(Format(props[i].GetValue(item)));
            rowIndex++;
        }

        if (columns.Count > 0)
        {
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents(1, 200, 8, 60);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"{ExportCatalog.Lists[listKey].Title}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return (stream.ToArray(), fileName);
    }

    /// <summary>一律輸出字串：這些盤點欄位本來就是自由文字，轉型只會製造格式落差。</summary>
    private static string Format(object? value) => value switch
    {
        null => "",
        bool b => b ? "是" : "否",
        // 不指定的話會跟著伺服器的地區設定變成「2026/9/11 下午 02:06:07」
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
        // 多對多欄位（使用資料(欄位)、特定目的）
        IEnumerable<PdCategory> categories => string.Join("、", categories.Select(c => c.Label)),
        IEnumerable<Purpose> purposes => string.Join("、", purposes.Select(p => p.Label)),
        IEnumerable e and not string => string.Join("、", e.Cast<object?>().Select(x => x?.ToString())),
        _ => value.ToString() ?? "",
    };

    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(c => !"[]:*?/\\".Contains(c)).ToArray());
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }
}

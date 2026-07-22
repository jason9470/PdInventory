# 個資盤點管理系統 (PdInventory)

取代人工維護之「個資清冊.xlsx」的 Web 系統。

## 技術架構

| 項目 | 內容 |
|------|------|
| 框架 | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core |
| 資料庫 | SQLite（`App_Data/pdinventory.db`，首次啟動自動建立） |
| 前端 | Bootstrap 5（專案範本內建） |

## 啟動方式

```bash
cd PdInventory
dotnet run --launch-profile http
# 瀏覽 http://localhost:5199
```

首次啟動時自動建立資料庫，並從 `Data/Seed/*.json`（由 個資清冊.xlsx 匯出）匯入初始資料。
若要重置資料庫，刪除 `App_Data/pdinventory.db` 後重新啟動即可。

## 功能對應

| Excel 工作表 | 系統功能 | 路徑 |
|--------------|---------|------|
| Sheet1 個人資料檔案盤點表(人為產出) | 盤點項目 CRUD、關鍵字搜尋 | `/Inventory` |
| Sheet2 系統自動拋轉清單(系統自動產出) | 拋轉紀錄 CRUD、拋入/拋出篩選 | `/Transfers` |
| Sheet3 資訊系統、資料庫與檔案伺服器盤點表 | 系統 CRUD | `/Systems` |
| 附表一 法務部公告個人資料類別 | 維護檔 CRUD（被引用時禁止刪除） | `/Categories` |
| 附表二 法務部公告特定目的列表 | 維護檔 CRUD（被引用時禁止刪除） | `/Purposes` |

## 資料正規化

原 Excel 的「使用資料(欄位)」「特定目的」為手動輸入文字，寫法混亂
（如 `COO三`、`C00三`、`Ｃ○○三` 混用）。本系統改為 **多對多關聯**：

- `InventoryItem` ⟷ `PdCategory`（使用資料欄位，勾選附表一代號）
- `InventoryItem` ⟷ `Purpose`（特定目的，勾選附表二代號）

匯入種子資料時已將所有混用寫法正規化為標準代號（內部代號 `C001`～`C134`，
顯示代號 `Ｃ○○一`～`Ｃ一三四`）。維護檔項目若仍被盤點項目引用，系統會擋下刪除。

## 專案結構

```
PdInventory/
├── Models/Entities.cs        # 5 個資料實體（含中文 Display 標籤）
├── Data/
│   ├── AppDbContext.cs       # EF Core DbContext（多對多設定）
│   ├── DbSeeder.cs           # 首次啟動種子資料匯入
│   └── Seed/*.json           # 由 個資清冊.xlsx 匯出之初始資料
├── Controllers/              # Home / Inventory / Transfers / Systems / Categories / Purposes
└── Views/                    # 各功能之 Index / Details / Form（Create 與 Edit 共用）
```

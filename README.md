# BizFlowLab — C# Minimal API × SQL Server Web APP 學習專案

這是以 **C# / ASP.NET Core Minimal API、Microsoft SQL Server、HTML、CSS 與 JavaScript** 建立的本機訂單管理練習專案。由網頁透過 `fetch()` 向 C# HTTP API 查詢訂單，後端再使用 ADO.NET 存取 SQL Server。

> **開發參與說明：** 這是一份 AI 輔助學習作品。約 99% 的原始 C# 程式由 GPT 提供；我負責提出學習需求、閱讀程式、少量手動修改、配置本機資料庫並親自進行 HTTP、SQL 與交易測試。Web UI 亦由 AI 協助建立。我正在持續學習 C# 與 Web 應用開發，**不將本專案宣稱為獨立完成的商業系統**。

## 功能與限制

| 項目 | 目前功能 |
| --- | --- |
| Web UI | 訂單列表、狀態篩選、單筆訂單摘要（僅供查詢） |
| `GET /health` | API 程序存活檢查；**不代表 SQL Server 健康** |
| `GET /api/orders` | 全部訂單或 `?status=Pending/Completed/Cancelled` 篩選 |
| `GET /api/orders/{orderId}` | 單筆訂單摘要，無資料時回傳 404 |
| `POST /api/orders` | 僅 Development 環境可使用；透過 JSON 建立訂單，具有輸入驗證、交易與錯誤處理 |
| SQL Server | Customer、Product、SalesOrder、SalesOrderItem 四張關聯資料表 |

**目前不包含訂單商品明細 API** `GET /api/orders/{orderId}/items`。該路由曾在私人學習副本中作為路由練習，但尚未接上 SQL JOIN，因此本公開候選版不收錄示範路由。網頁的「查看」只顯示訂單摘要。

> **重要：此專案只能用於本機學習。** 目前沒有使用者登入或授權機制；`Development` 限制並不能取代身分驗證。請勿將 API 直接部署到公開網路，或使用正式客戶資料。

## 技術與檔案結構

```text
BizFlowLab/
├── README.md
├── .gitignore
├── database/
│   ├── create.sql       # SSMS 匯出的 Schema only，UTF-8 版
│   └── seed_demo.sql    # 選用：只供空白練習資料庫的合成資料
├── src/BizFlowLab.Api/
│   ├── Program.cs       # C# Minimal API、ADO.NET 與 SQL Transaction
│   ├── BizFlowLab.Api.csproj
│   ├── appsettings.json
│   └── wwwroot/
│       ├── index.html
│       ├── styles.css
│       └── app.js
└── docs/
    ├── DATABASE_SCHEMA.md
    └── WEB_LESSON.md
```

- `Program.cs` 內使用 `MapGet`、`MapPost`、參數化 SQL 與 `SqlTransaction`。
- `wwwroot` 由 `UseDefaultFiles()`、`UseStaticFiles()` 提供靜態頁面；前後端共用同一個本機位址。
- SQL Server 的 `SalesOrderItem.UnitPrice` 儲存建立訂單當時的單價，避免歷史金額受商品目前定價影響。

## 本機啟動（Windows／PowerShell）

需要：**.NET 8 SDK**、可連線的 **Microsoft SQL Server**，以及 SSMS（方便操作腳本）。

1. 在 SSMS 確認用的是**獨立練習用資料庫**。如果還沒有 `BizFlowLab`，可先在 SSMS 執行 `CREATE DATABASE [BizFlowLab];`，再執行 [`database/create.sql`](database/create.sql) 建立四張表與約束。`create.sql` 本身以 `USE [BizFlowLab]` 開頭，**不會自動建立資料庫**。
2. `create.sql` **只適用尚未建表的空白資料庫**；不要在你原本已有資料的 `BizFlowLab` 再執行一次。若要展示查詢畫面，可在全新、空白的練習資料庫中，選擇執行 [`database/seed_demo.sql`](database/seed_demo.sql) 匯入合成客戶、商品及訂單。此步驟不適用於既有資料庫。
3. 使用 PowerShell 進入 `src/BizFlowLab.Api`，設定 **User Secrets** 連線字串（請更換伺服器名稱及驗證方式）。

```powershell
dotnet user-secrets set "ConnectionStrings:BizFlowLab" "Server=YOUR_SQL_SERVER;Database=BizFlowLab;Integrated Security=True;TrustServerCertificate=True"
```

4. 在相同目錄啟動（只監聽本機）：

```powershell
dotnet build
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile -- --urls http://127.0.0.1:5180
```

5. 在瀏覽器開啟 `http://127.0.0.1:5180/`。如果網頁可開啟但查詢失敗，先檢查 PowerShell 的資料庫錯誤訊息、User Secrets，以及 SQL Server 是否正在執行。

> `TrustServerCertificate=True` 只為簡化隔離的本機練習連線設定；不應直接當成正式部署的憑證信任配置。

## 已完成的人工練習與測試

以下是**在個人本機環境操作過的指定案例**，不是自動化測試套件、整體覆蓋率，也不代表重新克隆後一定通過。

- 已於本機成功啟動網頁，透過 F12 Network 觀察 `GET /api/orders` JSON 回應與狀態篩選（含查無資料時的 `200 []`）。
- 已驗證 `GET /health` → 200；不存在的訂單 ID → 404；不合法的 status → 400。
- 已測試 POST 的無效數量（`Quantity=0`），於交易建立前回傳 400。
- 已使用開發用測試資料建立訂單 `L5-OK-001`，並在網頁與 SQL Server 查詢到總額 900 元的紀錄。
- 已使用不存在的第二筆商品模擬寫入中途失敗，觀察到 HTTP 400、Rollback 日誌，並用 SQL 查詢確認該測試單號未留下訂單。
- 已在 SSMS 使用 JOIN 查詢商品明細，以及使用 `SUM` / `GROUP BY` 計算訂單總額。

測試時的 Console 與 SQL Server 不屬於此公開 Repository 的自動化驗證環境。本版本尚未在此輸出環境重新執行 .NET 編譯或 SQL Server 整合測試。

## 設計取捨與後續學習

- Web UI 僅唯讀，沒有新增／刪除訂單的網頁操作，但 Development API 仍提供 POST。
- 尚未實作登入、角色授權、正式部署、CI/CD 或完整自動化測試。
- 在私人學習副本中，已完成 `/api/orders/{orderId}/items` 的**路由練習**；資料庫查詢與網頁商品明細功能仍屬後續學習事項，不納入此公開候選版。
- SQL Server 結構來源與相依關係見 [`docs/DATABASE_SCHEMA.md`](docs/DATABASE_SCHEMA.md)，基礎操作步驟見 [`docs/WEB_LESSON.md`](docs/WEB_LESSON.md)。

## 注意事項

# 啟用開發環境
 - $env:DOTNET_ENVIRONMENT = "Development"

# 設定資料庫連線字串（首次設定時執行）
 - dotnet user-secrets set "ConnectionStrings:BizFlowLab" "Server=YOUR_SERVER;Database=BizFlowLab;Integrated Security=True;TrustServerCertificate=True"

# 啟動 Web APP
 - dotnet run --no-launch-profile -- --urls http://127.0.0.1:5180

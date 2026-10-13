# BizFlowLab — SQL Server 資料庫結構

實際結構依使用者透過 SSMS **Generate Scripts → Schema only** 匯出的 [`database/create.sql`](../database/create.sql) 為準，並非根據圖片推測的建表版本。公開候選版將原始 SSMS UTF-16LE 腳本**只轉換為 UTF-8**，沒有改寫 DDL 內容。

## 四張資料表

| 表 | 主鍵 | 重要欄位 |
| --- | --- | --- |
| `Customer` | `CustomerId int IDENTITY(1,1)` | `CustomerName nvarchar(80) NOT NULL` |
| `Product` | `ProductId int IDENTITY(1,1)` | `ProductCode varchar(20)`（UNIQUE）、`ProductName nvarchar(80)`、`UnitPrice decimal(18,2)` |
| `SalesOrder` | `OrderId int IDENTITY(1,1)` | `OrderNo varchar(20)`（UNIQUE）、`CustomerId int`、`OrderDate datetime2(0)`、`Status nvarchar(20)` |
| `SalesOrderItem` | `OrderItemId int IDENTITY(1,1)` | `OrderId int`、`ProductId int`、`Quantity int`、`UnitPrice decimal(18,2)` |

## 資料表關係

- `FK_SalesOrder_Customer`：`SalesOrder.CustomerId` → `Customer.CustomerId`
- `FK_SalesOrderItem_Order`：`SalesOrderItem.OrderId` → `SalesOrder.OrderId`
- `FK_SalesOrderItem_Product`：`SalesOrderItem.ProductId` → `Product.ProductId`

明細表另有 `(OrderId, ProductId)` 複合 UNIQUE 約束，避免同一訂單重複加入相同商品。`Quantity` 必須大於 0；`UnitPrice` 不能為負數；狀態限制在 `Pending / Completed / Cancelled`。

`SalesOrder.OrderDate` 預設 `SYSDATETIME()`，`Status` 預設 `Pending`，因此建立訂單 API 可以不明確傳入 OrderDate。

## 如何在空白環境還原

1. 先以 SSMS 建立空白資料庫 `BizFlowLab`，再執行 `database/create.sql`。SSMS 中的 `GO` 是批次分隔符，若使用一般 ADO.NET `SqlCommand`，不能將整份包含 `GO` 的腳本直接當作單一指令執行。
2. 這份 `create.sql` 包含四張表、PK／FK、UNIQUE、CHECK、DEFAULT 以及 IDENTITY，不包含客戶或訂單資料、密碼，也**不會自動建立 BizFlowLab 資料庫**。
3. 如需示範資料，僅在**全新的空白練習資料庫**執行 `database/seed_demo.sql`。腳本內有空表檢查，若已有資料會拒絕。
4. 若 SSMS／SQL Server 版本不支援匯出腳本中的索引選項（例如 `OPTIMIZE_FOR_SEQUENTIAL_KEY`），需對照你的資料庫版本另行調整；本候選版沒有擅改原始結構。

**勿在目前已存在訂單紀錄的個人 BizFlowLab 重新執行建表或種子資料腳本。**

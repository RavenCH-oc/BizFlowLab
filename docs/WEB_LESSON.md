# Web APP 入門練習：從畫面到 SQL Server

此練習基於現有的 C# API，不要求你一開始就理解全部 `Program.cs`。每一步都可以用瀏覽器 DevTools（F12）看到實際效果。

## 1. 四種技術各做什麼？

- **HTML**：決定有哪些標題、下拉選單、按鈕、表格和提示訊息。
- **CSS**：讓 HTML 元素有顏色、間距、版面與手機響應式布局。
- **JavaScript**：在點擊按鈕時呼叫 API，再把收到的資料放到頁面上。
- **C# Minimal API**：接收 HTTP Request、查 SQL Server，回傳 JSON Response。瀏覽器不直接連線 SQL Server。

資料流：`Browser → fetch('/api/orders') → C# MapGet → SQL Server → JSON → JavaScript 更新表格`。

## 2. 開始觀察：第一次打開首頁

1. 執行 README 的 `dotnet run`，使用 `http://127.0.0.1:5180/` 開啟首頁。
2. 開啟 F12 → Network，勾選 Preserve log 可觀察完整記錄。
3. 重新整理，分別觀察 `index.html`、`styles.css`、`app.js`、`api/orders`。
4. 選 `api/orders` 查看 **Headers → Request URL / Status Code** 及 **Response → JSON**。

思考：為什麼 `index.html` 是 HTML，而 `/api/orders` 回傳 JSON？

## 3. 看懂 HTML：元素與 ID

開啟 `wwwroot/index.html`，找：

```html
<select id="status-filter">...</select>
<button id="load-orders">重新查詢</button>
<tbody id="orders-body">...</tbody>
```

`id` 是讓 JS 找到元素的名稱。你可以先修改 `<h1>` 文字並刷新瀏覽器，確認你改的是 HTML 而非後端資料。

## 4. 看懂 JavaScript：`fetch` 與 `await`

開啟 `wwwroot/app.js`，找：

```js
const orders = await getJson("/api/orders");
```

上面是**示意**；實際程式會依下拉選單選擇適當 URL。`fetch()` 是瀏覽器的 HTTP 請求 API，`await` 讓非同步函式等待資料回來。C# 回傳的 JSON 會被解析成 JS 物件陣列。

改變 `status-filter`：

- 全部：`GET /api/orders`
- Pending：`GET /api/orders?status=Pending`
- Completed：`GET /api/orders?status=Completed`
- Cancelled：`GET /api/orders?status=Cancelled`

你可以在 DevTools Network 確認網址真的改變。查不到符合狀態的訂單時，`200 []` 是正常的「空集合」。

## 5. 看懂 DOM：資料怎麼出現在表格？

在 `renderOrders()` 裡，`document.createElement('tr')` 建立表格列，再將 `order.orderNo` 等值顯示出來。

使用 `textContent` 而不是直接把來自資料庫的文字塞進 `innerHTML`，可以避免資料被誤解成 HTML 標籤。試著修改第一個 `<th>` 的標題文字，不影響 API。

## 6. 看懂 HTTP 回應與錯誤

按列表的「查看」，會呼叫 `GET /api/orders/{id}`。你也可以在瀏覽器直接打：

```text
http://127.0.0.1:5180/health
http://127.0.0.1:5180/api/orders
http://127.0.0.1:5180/api/orders/1
http://127.0.0.1:5180/api/orders/9999
http://127.0.0.1:5180/api/orders?status=invalid
```

實際結果依資料庫現有資料而定：正常查詢是 200；單筆 ID 不存在可能是 404；不支援的 status 是 400；SQL 連線錯誤則可能是 500。

`fetch()` 本身不會因為 HTTP 400、404、500 而 throw；程式必須透過 `response.ok` 或 `response.status` 判斷。

## 7. 兩行 C# 中介軟體的作用

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
```

- `UseDefaultFiles()`：在請求 `/` 時尋找 `wwwroot/index.html` 等預設文件。
- `UseStaticFiles()`：回傳 `wwwroot/` 內的 HTML、CSS、JS 等靜態資源。

它們與原本的 `app.MapGet()` 不衝突：同一個本機 HTTP Server 可以回網頁，也可以回 JSON API，因而不必處理跨網域 CORS。

## 8. 三個練習（不需要改動資料庫）

- **練習 A（HTML）**：修改網頁標題，重整查看差異。
- **練習 B（CSS）**：修改 `.button` 背景色，觀察樣式選擇器。
- **練習 C（JavaScript）**：將列表的成功提示加入目前篩選條件名稱，再觀察 Network。

每完成一個練習，請用自己的話回答：改的是哪一層？有沒有發出 HTTP 請求？有沒有修改 SQL Server？

## 9. 下一階段再練習什麼？

目前先不要新增網頁 POST。等你了解 HTML / JS / JSON / API，再逐步做：

1. API 新增查詢訂單明細（JOIN SalesOrderItem、Product）。
2. 網頁新增「訂單明細」區塊。
3. 在測試資料庫做表單 POST，了解 JSON Body、伺服端驗證、201 與 400／409，以及 Transaction。

這些是**未實作的下一階段**，不能直接當成目前已完成的功能。

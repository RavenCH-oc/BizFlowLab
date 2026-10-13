using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// 在 Development 環境讀取本機 User Secrets；不要把連線設定硬寫進原始碼。
var connectionString = builder.Configuration.GetConnectionString("BizFlowLab");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:BizFlowLab. Check User Secrets and Development environment.");
}

var app = builder.Build();

// Web lesson: serve wwwroot/index.html on /, plus local CSS and JavaScript files.
// Existing /health and /api/orders endpoints below remain unchanged.
app.UseDefaultFiles();
app.UseStaticFiles();

// 這是存活檢查，沒有連接資料庫，不能代表 DB 正常。
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "BizFlowLab.Api"
}));

// Round 4：新增可選的 status 查詢條件；原本不帶參數的 GET 行為保持不變。
app.MapGet("/api/orders", async Task<IResult> (string? status, CancellationToken cancellationToken) =>
{
    // 缺少 status 表示查全部；有傳入但不合法則回 400。
    string? normalizedStatus = null;
    if (status is not null)
    {
        normalizedStatus = new[] { "Pending", "Completed", "Cancelled" }
            .FirstOrDefault(value => string.Equals(value, status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (normalizedStatus is null)
            return Results.BadRequest(new { message = "Status must be Pending, Completed or Cancelled." });
    }

    try
    {
        var orders = await QueryOrdersAsync(connectionString, null, normalizedStatus, cancellationToken);
        return Results.Ok(orders);
    }
    catch (SqlException ex)
    {
        // 詳細錯誤留在 API 主控台；不要把資料庫內部錯誤回傳給使用者。
        app.Logger.LogError(ex, "Order list query failed. SqlErrorNumber={SqlErrorNumber}", ex.Number);
        return Results.Problem(
            title: "Database query failed.",
            detail: "Check the API console log.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/api/orders/{orderId:int}",
    async Task<IResult> (int orderId, CancellationToken cancellationToken) =>
{
    if (orderId <= 0)
    {
        return Results.BadRequest(new { message = "OrderId must be greater than zero." });
    }

    try
    {
        var orders = await QueryOrdersAsync(connectionString, orderId, null, cancellationToken);
        var order = orders.FirstOrDefault();

        if (order is null)
        {
            // 只有「查詢成功但沒有這筆資料」才回傳 404。
            return Results.NotFound(new { message = "Order not found.", orderId });
        }

        return Results.Ok(order);
    }
    catch (SqlException ex)
    {
        app.Logger.LogError(ex,
            "Order query failed. OrderId={OrderId}, SqlErrorNumber={SqlErrorNumber}",
            orderId, ex.Number);
        return Results.Problem(
            title: "Database query failed.",
            detail: "Check the API console log.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

// Round 3：用 JSON 新增訂單。只在本機 Development Lab 使用，尚無登入／授權。
app.MapPost("/api/orders",
    async Task<IResult> (CreateOrderRequest request, CancellationToken cancellationToken) =>
{
    if (!app.Environment.IsDevelopment())
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    // 先驗證不需要查 DB 的資料；例如 Quantity=0，這裡就退回，不開交易。
    var validationError = ValidateCreateOrderRequest(request);
    if (validationError is not null)
        return Results.BadRequest(new { message = validationError });

    try
    {
        var created = await CreateOrderAsync(
            connectionString, request, app.Logger, cancellationToken);

        // 只有 Commit 成功返回後才回 201。Location 指向既有 GET 端點。
        return Results.Created($"/api/orders/{created.OrderId}", created);
    }
    catch (OrderInputException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
    catch (OrderKeyConflictException)
    {
        return Results.Conflict(new
        {
            message = "An order key conflicts with existing data. Check OrderNo.",
            orderNo = request.OrderNo
        });
    }
    catch (Exception ex)
    {
        // 不將原始 DB 錯誤或連線資訊回傳。不要在這裡自動重試寫入。
        app.Logger.LogError(ex, "Order creation did not complete normally. OrderNo={OrderNo}",
            request.OrderNo);
        return Results.Problem(
            title: "Order creation did not complete normally.",
            detail: "Check the API log and query by OrderNo before retrying.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.Run();

// 兩個 GET 訂單端點共用此方法。
// orderId=null 不限制訂單 ID；status=null 不限制狀態。所有輸入都使用 SQL 參數。
static async Task<List<OrderSummary>> QueryOrdersAsync(
    string connectionString,
    int? orderId,
    string? status,
    CancellationToken cancellationToken)
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync(cancellationToken);

    using var command = connection.CreateCommand();
    command.CommandTimeout = 10;
    command.CommandText = """
        SELECT
            so.OrderId,
            so.OrderNo,
            c.CustomerName,
            so.Status,
            CAST(COALESCE(SUM(i.Quantity * i.UnitPrice), 0) AS decimal(18,2)) AS TotalAmount
        FROM dbo.SalesOrder AS so
        INNER JOIN dbo.Customer AS c ON c.CustomerId = so.CustomerId
        LEFT JOIN dbo.SalesOrderItem AS i ON i.OrderId = so.OrderId
        WHERE (@OrderId IS NULL OR so.OrderId = @OrderId)
          AND (@Status IS NULL OR so.Status = @Status)
        GROUP BY so.OrderId, so.OrderNo, c.CustomerName, so.Status
        ORDER BY so.OrderId;
        """;

    // 固定 SQL + 有型別的參數；不把網址輸入拼進 SQL 文字。
    var idParameter = command.Parameters.Add("@OrderId", SqlDbType.Int);
    idParameter.Value = orderId.HasValue ? (object)orderId.Value : DBNull.Value;

    var statusParameter = command.Parameters.Add("@Status", SqlDbType.NVarChar, 20);
    statusParameter.Value = status is null ? DBNull.Value : (object)status;

    using var reader = await command.ExecuteReaderAsync(cancellationToken);
    var results = new List<OrderSummary>();

    while (await reader.ReadAsync(cancellationToken))
    {
        // 0~4 必須與上方 SELECT 欄位順序一致。
        results.Add(new OrderSummary(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetDecimal(4)
        ));
    }

    return results;
}

// 純輸入檢查：不連線、不修改資料庫。
static string? ValidateCreateOrderRequest(CreateOrderRequest request)
{
    // 本 Lab 單號由呼叫端提供；僅接受大寫英數與 -，長度配合 varchar(20)。
    if (string.IsNullOrWhiteSpace(request.OrderNo) || request.OrderNo.Length > 20 ||
        request.OrderNo.Any(c => !(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')))
        return "OrderNo must contain 1-20 uppercase letters, digits or hyphens.";

    if (request.CustomerId <= 0)
        return "CustomerId must be greater than zero.";

    if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 50)
        return "Items must contain 1-50 lines.";

    var seenProductIds = new HashSet<int>();
    foreach (var item in request.Items)
    {
        if (item is null)
            return "An item cannot be null.";
        if (item.ProductId <= 0)
            return "Each ProductId must be greater than zero.";
        if (item.Quantity < 1 || item.Quantity > 10000)
            return "Each Quantity must be between 1 and 10000.";
        if (!seenProductIds.Add(item.ProductId))
            return "The same ProductId cannot appear twice in one order; combine its quantity.";
    }

    return null;
}

// 主檔與全部明細使用「同一條 Connection、同一個 Transaction」。
static async Task<CreatedOrder> CreateOrderAsync(
    string connectionString,
    CreateOrderRequest request,
    ILogger logger,
    CancellationToken cancellationToken)
{
    // 防止誤用這個 Lab 寫入其他資料庫；不代表具備正式授權或安全部署能力。
    var settings = new SqlConnectionStringBuilder(connectionString);
    if (!string.Equals(settings.InitialCatalog, "BizFlowLab", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Round 3 writes are allowed only in BizFlowLab.");

    // 已經通過 ValidateCreateOrderRequest，故此處的 OrderNo、Items、item 非 null。
    var orderNo = request.OrderNo!;
    var items = request.Items!;

    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync(cancellationToken);
    using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

    int orderId = 0;
    int insertedItems = 0;
    decimal totalAmount = 0m;
    // 配合前一輪 GET 摘要的 decimal(18,2)。本輪無折扣、稅金、運費。
    const decimal maxOrderTotal = 9_999_999_999_999_999.99m;

    try
    {
        // A. 寫主檔。Customer 不存在時 SELECT 為 0 筆，不會新增主檔。
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandTimeout = 10;
            command.CommandText = """
                INSERT INTO dbo.SalesOrder (OrderNo, CustomerId, Status)
                OUTPUT INSERTED.OrderId
                SELECT @OrderNo, c.CustomerId, N'Pending'
                FROM dbo.Customer AS c
                WHERE c.CustomerId = @CustomerId;
                """;
            command.Parameters.Add("@OrderNo", SqlDbType.VarChar, 20).Value = orderNo;
            command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = request.CustomerId;

            object? value;
            try
            {
                value = await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                // 只在主檔 INSERT 的唯一鍵衝突轉成 409；不要把全部 SQL 錯誤都當重複單號。
                throw new OrderKeyConflictException(ex);
            }

            if (value is null || value == DBNull.Value)
                throw new OrderInputException($"CustomerId {request.CustomerId} does not exist.");
            orderId = (int)value;
        }

        logger.LogInformation(
            "R3: header inserted (uncommitted). OrderNo={OrderNo}, OrderId={OrderId}",
            orderNo, orderId);

        // B. 逐筆加入明細；單價由商品表讀取，不信任呼叫端傳來的金額。
        // 教學上在交易內逐筆確認商品，讓第二筆商品不存在時能觀察「中途失敗」的回復。
        foreach (var nullableItem in items)
        {
            var item = nullableItem!;
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 10;
            command.CommandText = """
                INSERT INTO dbo.SalesOrderItem (OrderId, ProductId, Quantity, UnitPrice)
                OUTPUT INSERTED.UnitPrice
                SELECT @OrderId, p.ProductId, @Quantity, p.UnitPrice
                FROM dbo.Product AS p
                WHERE p.ProductId = @ProductId;
                """;
            command.Parameters.Add("@OrderId", SqlDbType.Int).Value = orderId;
            command.Parameters.Add("@ProductId", SqlDbType.Int).Value = item.ProductId;
            command.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;

            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null || value == DBNull.Value)
                throw new OrderInputException($"ProductId {item.ProductId} does not exist.");

            var unitPrice = (decimal)value;
            insertedItems++;
            totalAmount += item.Quantity * unitPrice;
            if (totalAmount > maxOrderTotal)
                throw new OrderInputException("Order total exceeds the supported amount.");

            logger.LogInformation(
                "R3: item inserted (uncommitted). OrderNo={OrderNo}, ProductId={ProductId}, ItemsWritten={ItemsWritten}",
                orderNo, item.ProductId, insertedItems);
        }

        // C. 全部成功才提交。這裡不呼叫外部 API，不等待使用者按確認。
        await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception originalError)
    {
        try
        {
            // 即使 HTTP 請求已取消，也嘗試完成清理；不能沿用已取消的 request token。
            await transaction.RollbackAsync(CancellationToken.None);
            logger.LogWarning(
                "R3: transaction rolled back. OrderNo={OrderNo}, ItemsWrittenBeforeRollback={ItemsWritten}",
                orderNo, insertedItems);
        }
        catch (Exception rollbackError)
        {
            // 連線中斷或伺服器已結束交易時，Rollback 本身也可能失敗。
            // 不能因此宣稱資料一定不存在；轉成 500，保留兩個錯誤供排查。
            logger.LogError(rollbackError,
                "R3: rollback could not be confirmed. Inspect OrderNo={OrderNo} before retrying.",
                orderNo);
            throw new AggregateException(
                "Transaction outcome needs inspection.", originalError, rollbackError);
        }
        throw;
    }

    logger.LogInformation("R3: transaction committed. OrderNo={OrderNo}, OrderId={OrderId}",
        orderNo, orderId);
    return new CreatedOrder(orderId, orderNo, request.CustomerId, "Pending", insertedItems, totalAmount);
}

public sealed record OrderSummary(
    int OrderId,
    string OrderNo,
    string CustomerName,
    string Status,
    decimal TotalAmount
);

// POST 的 JSON Body 會對應到這兩種型別。單價與總額不由呼叫端決定。
public sealed record CreateOrderRequest(
    string? OrderNo,
    int CustomerId,
    List<CreateOrderItemRequest?>? Items
);

public sealed record CreateOrderItemRequest(int ProductId, int Quantity);

public sealed record CreatedOrder(
    int OrderId,
    string OrderNo,
    int CustomerId,
    string Status,
    int ItemCount,
    decimal TotalAmount
);

public sealed class OrderInputException : Exception
{
    public OrderInputException(string message) : base(message) { }
}

public sealed class OrderKeyConflictException : Exception
{
    public OrderKeyConflictException(Exception inner)
        : base("Order key conflict.", inner) { }
}

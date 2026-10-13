-- BizFlowLab 合成示範資料。只適用執行 create.sql 後的全新空白資料庫。
-- 禁止在已經有個人學習訂單的資料庫執行。
USE [BizFlowLab];
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM dbo.Customer)
       OR EXISTS (SELECT 1 FROM dbo.Product)
       OR EXISTS (SELECT 1 FROM dbo.SalesOrder)
       OR EXISTS (SELECT 1 FROM dbo.SalesOrderItem)
    BEGIN
        THROW 50001, N'seed_demo.sql only supports an empty BizFlowLab database.', 1;
    END;

    INSERT INTO dbo.Customer (CustomerName)
    VALUES (N'DemoCustomerA'), (N'DemoCustomerB'), (N'DemoCustomerC');

    INSERT INTO dbo.Product (ProductCode, ProductName, UnitPrice)
    VALUES ('P001', N'DemoSet', 300.00),
           ('P002', N'DemoDrink', 100.00);

    INSERT INTO dbo.SalesOrder (OrderNo, CustomerId, Status)
    SELECT 'SO0001', CustomerId, N'Pending' FROM dbo.Customer WHERE CustomerName = N'DemoCustomerA';
    INSERT INTO dbo.SalesOrder (OrderNo, CustomerId, Status)
    SELECT 'SO0002', CustomerId, N'Completed' FROM dbo.Customer WHERE CustomerName = N'DemoCustomerB';
    INSERT INTO dbo.SalesOrder (OrderNo, CustomerId, Status)
    SELECT 'SO0003', CustomerId, N'Pending' FROM dbo.Customer WHERE CustomerName = N'DemoCustomerC';

    INSERT INTO dbo.SalesOrderItem (OrderId, ProductId, Quantity, UnitPrice)
    SELECT so.OrderId, p.ProductId, v.Quantity, p.UnitPrice
    FROM (VALUES
      ('SO0001', 'P001', 2),
      ('SO0001', 'P002', 6),
      ('SO0002', 'P001', 2),
      ('SO0003', 'P001', 3)
    ) AS v(OrderNo, ProductCode, Quantity)
    JOIN dbo.SalesOrder AS so ON so.OrderNo = v.OrderNo
    JOIN dbo.Product AS p ON p.ProductCode = v.ProductCode;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

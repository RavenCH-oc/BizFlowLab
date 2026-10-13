USE [BizFlowLab]
GO
/****** 物件:  Table [dbo].[Customer]    指令碼日期: 2026/10/13 下午 08:34:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customer](
	[CustomerId] [int] IDENTITY(1,1) NOT NULL,
	[CustomerName] [nvarchar](80) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Product]    指令碼日期: 2026/10/13 下午 08:34:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[ProductId] [int] IDENTITY(1,1) NOT NULL,
	[ProductCode] [varchar](20) NOT NULL,
	[ProductName] [nvarchar](80) NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ProductCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[SalesOrder]    指令碼日期: 2026/10/13 下午 08:34:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SalesOrder](
	[OrderId] [int] IDENTITY(1,1) NOT NULL,
	[OrderNo] [varchar](20) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[OrderDate] [datetime2](0) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[OrderNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[SalesOrderItem]    指令碼日期: 2026/10/13 下午 08:34:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SalesOrderItem](
	[OrderItemId] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[ProductId] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_SalesOrderItem_Order_Product] UNIQUE NONCLUSTERED 
(
	[OrderId] ASC,
	[ProductId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SalesOrder] ADD  CONSTRAINT [DF_SalesOrder_OrderDate]  DEFAULT (sysdatetime()) FOR [OrderDate]
GO
ALTER TABLE [dbo].[SalesOrder] ADD  CONSTRAINT [DF_SalesOrder_Status]  DEFAULT (N'Pending') FOR [Status]
GO
ALTER TABLE [dbo].[SalesOrder]  WITH CHECK ADD  CONSTRAINT [FK_SalesOrder_Customer] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customer] ([CustomerId])
GO
ALTER TABLE [dbo].[SalesOrder] CHECK CONSTRAINT [FK_SalesOrder_Customer]
GO
ALTER TABLE [dbo].[SalesOrderItem]  WITH CHECK ADD  CONSTRAINT [FK_SalesOrderItem_Order] FOREIGN KEY([OrderId])
REFERENCES [dbo].[SalesOrder] ([OrderId])
GO
ALTER TABLE [dbo].[SalesOrderItem] CHECK CONSTRAINT [FK_SalesOrderItem_Order]
GO
ALTER TABLE [dbo].[SalesOrderItem]  WITH CHECK ADD  CONSTRAINT [FK_SalesOrderItem_Product] FOREIGN KEY([ProductId])
REFERENCES [dbo].[Product] ([ProductId])
GO
ALTER TABLE [dbo].[SalesOrderItem] CHECK CONSTRAINT [FK_SalesOrderItem_Product]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [CK_Product_UnitPrice] CHECK  (([UnitPrice]>=(0)))
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [CK_Product_UnitPrice]
GO
ALTER TABLE [dbo].[SalesOrder]  WITH CHECK ADD  CONSTRAINT [CK_SalesOrder_Status] CHECK  (([Status]=N'Cancelled' OR [Status]=N'Completed' OR [Status]=N'Pending'))
GO
ALTER TABLE [dbo].[SalesOrder] CHECK CONSTRAINT [CK_SalesOrder_Status]
GO
ALTER TABLE [dbo].[SalesOrderItem]  WITH CHECK ADD  CONSTRAINT [CK_SalesOrderItem_Quantity] CHECK  (([Quantity]>(0)))
GO
ALTER TABLE [dbo].[SalesOrderItem] CHECK CONSTRAINT [CK_SalesOrderItem_Quantity]
GO
ALTER TABLE [dbo].[SalesOrderItem]  WITH CHECK ADD  CONSTRAINT [CK_SalesOrderItem_UnitPrice] CHECK  (([UnitPrice]>=(0)))
GO
ALTER TABLE [dbo].[SalesOrderItem] CHECK CONSTRAINT [CK_SalesOrderItem_UnitPrice]
GO

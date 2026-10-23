-- Sample data for local development. Only runs against empty tables.

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name, Observation)
    VALUES (N'Lácteos', N'Leche, quesos, yogures y derivados'),
           (N'Granos', N'Arroz, frijol, lenteja y cereales'),
           (N'Bebidas', N'Gaseosas, jugos y agua'),
           (N'Aseo', N'Limpieza del hogar y cuidado personal');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Providers)
BEGIN
    INSERT INTO dbo.Providers (Name, Observation)
    VALUES (N'Distribuidora del Valle', N'Entrega martes y viernes'),
           (N'Alimentos La Granja', N'Proveedor de lácteos'),
           (N'Comercializadora Andina', NULL);
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PaymentMethods)
BEGIN
    INSERT INTO dbo.PaymentMethods (Name, Observation)
    VALUES (N'Efectivo', NULL),
           (N'Tarjeta débito', N'Datáfono en caja 1 y 2'),
           (N'Tarjeta crédito', N'Datáfono en caja 1 y 2'),
           (N'Transferencia', N'Pago con código QR');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT INTO dbo.Products (Name, Price, Stock, MinimumStock, CategoryId)
    SELECT seed.Name, seed.Price, seed.Stock, seed.MinimumStock, c.Id
    FROM (VALUES (N'Leche entera 1 L', 4200.00, 48, 20, N'Lácteos'),
                 (N'Queso campesino 500 g', 11500.00, 6, 8, N'Lácteos'),
                 (N'Arroz 1 kg', 4800.00, 60, 25, N'Granos'),
                 (N'Lenteja 500 g', 3900.00, 35, 10, N'Granos'),
                 (N'Agua sin gas 600 ml', 1800.00, 120, 40, N'Bebidas'),
                 (N'Jabón de loza 500 g', 5600.00, 4, 5, N'Aseo')) AS seed (Name, Price, Stock, MinimumStock, CategoryName)
    INNER JOIN dbo.Categories AS c ON c.Name = seed.CategoryName;

    UPDATE p
    SET ProviderId = pr.Id
    FROM dbo.Products AS p
    INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
    INNER JOIN dbo.Providers AS pr
        ON pr.Name = CASE c.Name
                         WHEN N'Lácteos' THEN N'Alimentos La Granja'
                         WHEN N'Granos' THEN N'Distribuidora del Valle'
                         WHEN N'Bebidas' THEN N'Comercializadora Andina'
                     END;

    -- A few past sales so the history is not empty. Totals use the seeded prices.
    DECLARE @cash INT = (SELECT Id FROM dbo.PaymentMethods WHERE Name = N'Efectivo');
    DECLARE @debit INT = (SELECT Id FROM dbo.PaymentMethods WHERE Name = N'Tarjeta débito');

    IF @cash IS NOT NULL AND @debit IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Sales)
    BEGIN
        DECLARE @sales TABLE (SaleNo INT, PaymentMethodId INT, HoursAgo INT);
        INSERT INTO @sales VALUES (1, @cash, 26), (2, @debit, 5), (3, @cash, 1);

        DECLARE @lines TABLE (SaleNo INT, ProductName NVARCHAR(100), Quantity INT);
        INSERT INTO @lines
        VALUES (1, N'Leche entera 1 L', 2), (1, N'Arroz 1 kg', 1),
               (2, N'Agua sin gas 600 ml', 6),
               (3, N'Queso campesino 500 g', 1), (3, N'Lenteja 500 g', 2);

        DECLARE @saleNo INT = 1, @saleId INT;
        WHILE @saleNo <= 3
        BEGIN
            INSERT INTO dbo.Sales (CreatedAt, PaymentMethodId, Total)
            SELECT DATEADD(HOUR, -s.HoursAgo, SYSUTCDATETIME()), s.PaymentMethodId,
                   (SELECT SUM(l.Quantity * p.Price)
                    FROM @lines AS l
                    INNER JOIN dbo.Products AS p ON p.Name = l.ProductName
                    WHERE l.SaleNo = s.SaleNo)
            FROM @sales AS s
            WHERE s.SaleNo = @saleNo;

            SET @saleId = SCOPE_IDENTITY();

            INSERT INTO dbo.SaleLines (SaleId, ProductId, Quantity, UnitPrice)
            SELECT @saleId, p.Id, l.Quantity, p.Price
            FROM @lines AS l
            INNER JOIN dbo.Products AS p ON p.Name = l.ProductName
            WHERE l.SaleNo = @saleNo;

            SET @saleNo += 1;
        END;
    END;
END;
GO

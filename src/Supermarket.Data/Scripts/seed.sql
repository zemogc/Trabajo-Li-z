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
END;
GO

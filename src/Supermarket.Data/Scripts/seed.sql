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
    INSERT INTO dbo.Products (Name, Price, Stock, CategoryId)
    SELECT seed.Name, seed.Price, seed.Stock, c.Id
    FROM (VALUES (N'Leche entera 1 L', 4200.00, 48, N'Lácteos'),
                 (N'Queso campesino 500 g', 11500.00, 20, N'Lácteos'),
                 (N'Arroz 1 kg', 4800.00, 60, N'Granos'),
                 (N'Lenteja 500 g', 3900.00, 35, N'Granos'),
                 (N'Agua sin gas 600 ml', 1800.00, 120, N'Bebidas'),
                 (N'Jabón de loza 500 g', 5600.00, 25, N'Aseo')) AS seed (Name, Price, Stock, CategoryName)
    INNER JOIN dbo.Categories AS c ON c.Name = seed.CategoryName;
END;
GO

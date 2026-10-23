-- Supermarket schema. Idempotent: safe to run on every start-up.

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id          INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name        NVARCHAR(50)       NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
        Observation NVARCHAR(200)      NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Providers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Providers
    (
        Id          INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Providers PRIMARY KEY,
        Name        NVARCHAR(50)       NOT NULL CONSTRAINT UQ_Providers_Name UNIQUE,
        Observation NVARCHAR(200)      NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PaymentMethods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentMethods
    (
        Id          INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_PaymentMethods PRIMARY KEY,
        Name        NVARCHAR(50)       NOT NULL CONSTRAINT UQ_PaymentMethods_Name UNIQUE,
        Observation NVARCHAR(200)      NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name       NVARCHAR(100)      NOT NULL CONSTRAINT UQ_Products_Name UNIQUE,
        Price      DECIMAL(18, 2)     NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
        Stock      INT                NOT NULL CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
        MinimumStock INT              NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT (5)
                                               CONSTRAINT CK_Products_MinimumStock CHECK (MinimumStock >= 0),
        CategoryId INT                NOT NULL
            CONSTRAINT FK_Products_Categories REFERENCES dbo.Categories (Id)
    );

    CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
END;
GO

-- v2: reorder level per product. Upgrades databases created before the column existed.
IF COL_LENGTH(N'dbo.Products', N'MinimumStock') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD MinimumStock INT NOT NULL
            CONSTRAINT DF_Products_MinimumStock DEFAULT (5)
            CONSTRAINT CK_Products_MinimumStock CHECK (MinimumStock >= 0);
END;
GO

-- v3: optional usual provider per product.
IF COL_LENGTH(N'dbo.Products', N'ProviderId') IS NULL
BEGIN
    ALTER TABLE dbo.Products
        ADD ProviderId INT NULL
            CONSTRAINT FK_Products_Providers REFERENCES dbo.Providers (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_ProviderId' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE INDEX IX_Products_ProviderId ON dbo.Products (ProviderId);
END;
GO

-- v4: sales. Lines keep the unit price of the moment of the sale.
IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sales
    (
        Id              INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
        CreatedAt       DATETIME2(0)       NOT NULL CONSTRAINT DF_Sales_CreatedAt DEFAULT (SYSUTCDATETIME()),
        PaymentMethodId INT                NOT NULL
            CONSTRAINT FK_Sales_PaymentMethods REFERENCES dbo.PaymentMethods (Id),
        Total           DECIMAL(18, 2)     NOT NULL CONSTRAINT CK_Sales_Total CHECK (Total > 0)
    );

    CREATE INDEX IX_Sales_CreatedAt ON dbo.Sales (CreatedAt DESC);
    CREATE INDEX IX_Sales_PaymentMethodId ON dbo.Sales (PaymentMethodId);
END;
GO

IF OBJECT_ID(N'dbo.SaleLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleLines
    (
        SaleId    INT            NOT NULL
            CONSTRAINT FK_SaleLines_Sales REFERENCES dbo.Sales (Id) ON DELETE CASCADE,
        ProductId INT            NOT NULL
            CONSTRAINT FK_SaleLines_Products REFERENCES dbo.Products (Id),
        Quantity  INT            NOT NULL CONSTRAINT CK_SaleLines_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18, 2) NOT NULL CONSTRAINT CK_SaleLines_UnitPrice CHECK (UnitPrice > 0),
        CONSTRAINT PK_SaleLines PRIMARY KEY (SaleId, ProductId)
    );

    CREATE INDEX IX_SaleLines_ProductId ON dbo.SaleLines (ProductId);
END;
GO

-- v5: voided sales stay in the history, marked with the moment they were voided.
IF COL_LENGTH(N'dbo.Sales', N'CancelledAt') IS NULL
BEGIN
    ALTER TABLE dbo.Sales ADD CancelledAt DATETIME2(0) NULL;
END;
GO

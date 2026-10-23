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

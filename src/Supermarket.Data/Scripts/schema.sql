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
        CategoryId INT                NOT NULL
            CONSTRAINT FK_Products_Categories REFERENCES dbo.Categories (Id)
    );

    CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
END;
GO

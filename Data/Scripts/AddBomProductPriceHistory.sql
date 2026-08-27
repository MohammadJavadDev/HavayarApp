SET XACT_ABORT ON;

IF OBJECT_ID(N'[Bom].[ProductPriceHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [Bom].[ProductPriceHistory]
    (
        [Id] BIGINT IDENTITY(1, 1) NOT NULL,
        [ProductFormulId] BIGINT NOT NULL,
        [ProductId] BIGINT NOT NULL,
        [PartCode] NVARCHAR(450) NULL,
        [PartName] NVARCHAR(MAX) NULL,
        [SnapshotDate] DATE NOT NULL,
        [SnapshotShamsiDate] NVARCHAR(10) NOT NULL,
        [CurrencyRate] DECIMAL(18, 2) NOT NULL,
        [InternalBuyPrice] DECIMAL(28, 5) NOT NULL,
        [ExternalBuyPrice] DECIMAL(28, 5) NOT NULL,
        [BuyPrice] DECIMAL(28, 5) NOT NULL,
        [BuyPriceInEuro] DECIMAL(28, 5) NOT NULL,
        [SalePrice] DECIMAL(28, 5) NOT NULL,
        [ComponentCount] INT NOT NULL,
        [PricedComponentCount] INT NOT NULL,
        [EmailSentOnMiladiDateTime] DATETIME2 NULL,
        [EmailSentOnShamsiDateTime] NVARCHAR(30) NULL,
        [CreatedById] BIGINT NULL,
        [ModifiedById] BIGINT NULL,
        [CreatedByName] NVARCHAR(150) NULL,
        [ModifiedByName] NVARCHAR(150) NULL,
        [ModifiedDateMiladiDateTime] DATETIME2 NULL,
        [ModifiedDateShamsiDateTime] NVARCHAR(30) NULL,
        [CreatedOnMiladiDateTime] DATETIME2 NULL,
        [CreatedOnShamsiDateTime] NVARCHAR(30) NULL,
        [IsActive] INT NULL,
        CONSTRAINT [PK_ProductPriceHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductPriceHistory_ProductFormul_ProductFormulId]
            FOREIGN KEY ([ProductFormulId]) REFERENCES [Bom].[ProductFormul] ([Id])
    );

    CREATE INDEX [IX_ProductPriceHistory_ProductFormulId_SnapshotDate]
        ON [Bom].[ProductPriceHistory] ([ProductFormulId], [SnapshotDate]);

    CREATE INDEX [IX_ProductPriceHistory_ProductId]
        ON [Bom].[ProductPriceHistory] ([ProductId]);

    CREATE UNIQUE INDEX [IX_ProductPriceHistory_SnapshotDate_ProductFormulId]
        ON [Bom].[ProductPriceHistory] ([SnapshotDate], [ProductFormulId]);
END;

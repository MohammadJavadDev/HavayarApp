-- Adds HtsId to Sup.OpenOrderRequestAttachment for idempotent attachment migration from TotalSystem
IF COL_LENGTH('Sup.OpenOrderRequestAttachment', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Sup].[OpenOrderRequestAttachment]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_OpenOrderRequestAttachment_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Sup.OpenOrderRequestAttachment';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Sup.OpenOrderRequestAttachment';
END
GO

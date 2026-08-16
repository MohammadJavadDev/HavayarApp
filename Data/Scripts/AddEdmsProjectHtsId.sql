-- Adds HtsId to Edms project tables for idempotent sync from TotalSystem
IF COL_LENGTH('Edms.Project', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[Project]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_Edms_Project_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Edms.Project';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Edms.Project';
END
GO

IF COL_LENGTH('Edms.ProjectProgressPercentage', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[ProjectProgressPercentage]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_Edms_ProjectProgressPercentage_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Edms.ProjectProgressPercentage';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Edms.ProjectProgressPercentage';
END
GO

IF COL_LENGTH('Edms.ProjectAttachment', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[ProjectAttachment]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_Edms_ProjectAttachment_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Edms.ProjectAttachment';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Edms.ProjectAttachment';
END
GO

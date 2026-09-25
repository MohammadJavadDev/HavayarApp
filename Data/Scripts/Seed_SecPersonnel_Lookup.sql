/*
Seed_SecPersonnel_Lookup.sql
جداول پایه دبیرخانه: EmailType و ReciversGroup با شناسه‌های ثابت HTS.
UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-personnel-lookup';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'Sec.EmailType', N'U') IS NULL
        THROW 51001, N'Sec.EmailType وجود ندارد — ابتدا migration را اعمال کنید.', 1;
    IF OBJECT_ID(N'Sec.ReciversGroup', N'U') IS NULL
        THROW 51002, N'Sec.ReciversGroup وجود ندارد — ابتدا migration را اعمال کنید.', 1;

    PRINT N'=== EmailType ===';
    IF NOT EXISTS (SELECT 1 FROM Sec.EmailType WHERE Id = 1)
    BEGIN
        SET IDENTITY_INSERT Sec.EmailType ON;
        INSERT INTO Sec.EmailType (Id, EmailTypeTitle, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (1, N'تبریک تولد', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1),
            (2, N'تبریک سالروز حضور', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET IDENTITY_INSERT Sec.EmailType OFF;
        PRINT N'  CREATED EmailType 1,2';
    END
    ELSE
    BEGIN
        UPDATE Sec.EmailType SET EmailTypeTitle = N'تبریک تولد', IsActive = 1 WHERE Id = 1;
        UPDATE Sec.EmailType SET EmailTypeTitle = N'تبریک سالروز حضور', IsActive = 1 WHERE Id = 2;
        IF NOT EXISTS (SELECT 1 FROM Sec.EmailType WHERE Id = 2)
        BEGIN
            SET IDENTITY_INSERT Sec.EmailType ON;
            INSERT INTO Sec.EmailType (Id, EmailTypeTitle, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (2, N'تبریک سالروز حضور', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET IDENTITY_INSERT Sec.EmailType OFF;
        END
        PRINT N'  UPDATED EmailType';
    END

    PRINT N'=== ReciversGroup ===';
    IF NOT EXISTS (SELECT 1 FROM Sec.ReciversGroup WHERE Id = 1)
    BEGIN
        SET IDENTITY_INSERT Sec.ReciversGroup ON;
        INSERT INTO Sec.ReciversGroup (Id, ReciversEmails, ReciversTitle, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (1, N'co@havayar.com', N'دفتر مرکزی', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1),
            (2, N'factory@havayar.com', N'کارخانه', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1),
            (3, N'everyone@havayar.com', N'همه', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET IDENTITY_INSERT Sec.ReciversGroup OFF;
        PRINT N'  CREATED ReciversGroup 1,2,3';
    END
    ELSE
    BEGIN
        UPDATE Sec.ReciversGroup SET ReciversEmails = N'co@havayar.com', ReciversTitle = N'دفتر مرکزی', IsActive = 1 WHERE Id = 1;
        UPDATE Sec.ReciversGroup SET ReciversEmails = N'factory@havayar.com', ReciversTitle = N'کارخانه', IsActive = 1 WHERE Id = 2;
        UPDATE Sec.ReciversGroup SET ReciversEmails = N'everyone@havayar.com', ReciversTitle = N'همه', IsActive = 1 WHERE Id = 3;
        IF NOT EXISTS (SELECT 1 FROM Sec.ReciversGroup WHERE Id = 2)
        BEGIN
            SET IDENTITY_INSERT Sec.ReciversGroup ON;
            INSERT INTO Sec.ReciversGroup (Id, ReciversEmails, ReciversTitle, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (2, N'factory@havayar.com', N'کارخانه', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET IDENTITY_INSERT Sec.ReciversGroup OFF;
        END
        IF NOT EXISTS (SELECT 1 FROM Sec.ReciversGroup WHERE Id = 3)
        BEGIN
            SET IDENTITY_INSERT Sec.ReciversGroup ON;
            INSERT INTO Sec.ReciversGroup (Id, ReciversEmails, ReciversTitle, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (3, N'everyone@havayar.com', N'همه', 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET IDENTITY_INSERT Sec.ReciversGroup OFF;
        END
        PRINT N'  UPDATED ReciversGroup';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SecPersonnel_Lookup ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

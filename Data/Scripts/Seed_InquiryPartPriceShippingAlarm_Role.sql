/*
================================================================================
Seed_InquiryPartPriceShippingAlarm_Role.sql
نقش رونوشت ایمیل هشدار قیمت حمل. معادل گروه HTS 577.
شناسه قراردادی: 260000 + 577 = 260577
این نقش دسترسی صفحه ندارد؛ فقط عضویت برای جاب InquiryPartPriceShippingAlarmJob است.
UTF-8 BOM.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_InquiryPartPriceShippingAlarm_Role.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-shipping-price-alarm';
DECLARE @RoleId BIGINT = 260577;
DECLARE @RoleName NVARCHAR(200) = N'دریافت کنندگان ایمیل هشدار اختلاف قیمت خرید';

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @RoleName)
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = @RoleId)
        BEGIN
            SET IDENTITY_INSERT [system].[Role] ON;
            INSERT INTO [system].[Role]
                (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@RoleId, @RoleName, @RoleName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT [system].[Role] OFF;
        END
        ELSE
            INSERT INTO [system].[Role]
                (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                 CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@RoleName, @RoleName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
    RETURN;
END CATCH

SELECT Id, Name, Title, IsActive
FROM [system].[Role]
WHERE Name = N'دریافت کنندگان ایمیل هشدار اختلاف قیمت خرید';

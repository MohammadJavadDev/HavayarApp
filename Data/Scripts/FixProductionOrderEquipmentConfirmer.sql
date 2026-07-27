/*
================================================================================
اصلاح ساختار Pln.ProductionOrderEquipmentConfirmer مطابق مکانیزم واقعی HTS
================================================================================
زمینه:
  در HTS، Pln_ProductionOrderEquipmentConfirmer یک جدول تنظیمات سراسری است (نه
  به‌ازای هر سفارش ساخت) که برای هر LookupId (نوع دستگاه/Equipment، LookupType=52)
  یک MechanicalUserId و یک ElectricalUserId تعریف می‌کند. این نگاشت برای مسیریابی/
  اطلاع‌رسانی هدفمند وقتی اقلام در کارتابل مهندسی (SeenToMechanicalEngineering/
  SeenToElectricalEngineering) بیش از حد مجاز معطل می‌مانند استفاده می‌شود
  (ProductionOrderItemService.SendExpiredOrdersNotification در HTS).

  پیاده‌سازی قبلی این جدول در HavayarApp کاملاً مفهوم متفاوتی داشت: رکورد تایید
  به‌ازای هر (ProductionOrderId, ProductionOrderItemId) با یک ConfirmerUserId و
  IsConfirmed/ConfirmedOnDate - که نه با HTS مطابقت داشت و نه در جایی از منطق
  اعلان/مسیریابی واقعی استفاده می‌شد (ProductionOrderJob.SendInitilizeEmailNotification
  فقط یک TODO خالی بود).

این اسکریپت:
  1) اگر ستون‌های نسخه قبلی (ProductionOrderId/ProductionOrderItemId/ConfirmerUserId/
     IsConfirmed/ConfirmedOnMiladiDate/ConfirmedOnShamsiDate) وجود داشته باشند:
     - تمام رکوردهای موجود را حذف می‌کند (داده‌های قبلی با مفهوم جدید سازگار نیستند
       و صرفاً رکورد تست/ناقص بوده‌اند - نه تنظیمات معتبر تاییدکننده تجهیز)
     - تمام Foreign Key هایی که به این ستون‌ها اشاره دارند را پویا پیدا و حذف می‌کند
     - خود ستون‌ها را حذف می‌کند
  2) ستون‌های جدید (DeviceType، MechanicalUserId، ElectricalUserId) را اگر وجود
     نداشته باشند اضافه می‌کند
  3) Foreign Key های جدید (MechanicalUserId/ElectricalUserId → system.[User]) را
     اگر وجود نداشته باشند اضافه می‌کند

Idempotent است؛ اجرای مجدد آن مشکلی ایجاد نمی‌کند.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

	IF OBJECT_ID('Pln.ProductionOrderEquipmentConfirmer', 'U') IS NULL
		THROW 51000, N'جدول Pln.ProductionOrderEquipmentConfirmer یافت نشد.', 1;

	-----------------------------------------------------------------------------
	-- 1) پاک‌سازی ساختار نسخه قبلی (در صورت وجود)
	-----------------------------------------------------------------------------
	IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ConfirmerUserId') IS NOT NULL
	BEGIN
		DECLARE @DeletedCount INT;
		SELECT @DeletedCount = COUNT(*) FROM Pln.ProductionOrderEquipmentConfirmer;

		DELETE FROM Pln.ProductionOrderEquipmentConfirmer;
		PRINT N'تعداد ' + CAST(@DeletedCount AS NVARCHAR(20)) + N' رکورد با ساختار قبلی (ناسازگار با مفهوم جدید) حذف شد.';

		-- حذف پویای همه FK هایی که از این جدول به بیرون اشاره دارند (ProductionOrder/ProductionOrderItem/User)
		DECLARE @fkName NVARCHAR(256);
		DECLARE fk_cursor CURSOR LOCAL FAST_FORWARD FOR
			SELECT fk.name
			FROM sys.foreign_keys fk
			WHERE fk.parent_object_id = OBJECT_ID('Pln.ProductionOrderEquipmentConfirmer');

		OPEN fk_cursor;
		FETCH NEXT FROM fk_cursor INTO @fkName;
		WHILE @@FETCH_STATUS = 0
		BEGIN
			EXEC('ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP CONSTRAINT [' + @fkName + ']');
			PRINT N'FK حذف شد: ' + @fkName;
			FETCH NEXT FROM fk_cursor INTO @fkName;
		END
		CLOSE fk_cursor;
		DEALLOCATE fk_cursor;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ProductionOrderId') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN ProductionOrderId;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ProductionOrderItemId') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN ProductionOrderItemId;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ConfirmerUserId') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN ConfirmerUserId;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'IsConfirmed') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN IsConfirmed;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ConfirmedOnMiladiDate') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN ConfirmedOnMiladiDate;

		IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ConfirmedOnShamsiDate') IS NOT NULL
			ALTER TABLE Pln.ProductionOrderEquipmentConfirmer DROP COLUMN ConfirmedOnShamsiDate;

		PRINT N'ستون‌های نسخه قبلی حذف شدند.';
	END
	ELSE
	BEGIN
		PRINT N'ساختار قبلی یافت نشد (احتمالاً قبلاً migrate شده)؛ رد شدن از مرحله پاک‌سازی.';
	END

	-----------------------------------------------------------------------------
	-- 2) افزودن ستون‌های جدید
	-----------------------------------------------------------------------------
	IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'DeviceType') IS NULL
	BEGIN
		ALTER TABLE Pln.ProductionOrderEquipmentConfirmer ADD DeviceType INT NOT NULL CONSTRAINT DF_ProductionOrderEquipmentConfirmer_DeviceType DEFAULT (0);
		PRINT N'ستون DeviceType اضافه شد.';
	END

	IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'MechanicalUserId') IS NULL
	BEGIN
		ALTER TABLE Pln.ProductionOrderEquipmentConfirmer ADD MechanicalUserId BIGINT NULL;
		PRINT N'ستون MechanicalUserId اضافه شد.';
	END

	IF COL_LENGTH('Pln.ProductionOrderEquipmentConfirmer', 'ElectricalUserId') IS NULL
	BEGIN
		ALTER TABLE Pln.ProductionOrderEquipmentConfirmer ADD ElectricalUserId BIGINT NULL;
		PRINT N'ستون ElectricalUserId اضافه شد.';
	END

	-----------------------------------------------------------------------------
	-- 3) افزودن Foreign Key های جدید (در صورت عدم وجود)
	-----------------------------------------------------------------------------
	IF NOT EXISTS (
		SELECT 1 FROM sys.foreign_keys
		WHERE name = 'FK_ProductionOrderEquipmentConfirmer_MechanicalUser'
	)
	BEGIN
		ALTER TABLE Pln.ProductionOrderEquipmentConfirmer
			ADD CONSTRAINT FK_ProductionOrderEquipmentConfirmer_MechanicalUser
			FOREIGN KEY (MechanicalUserId) REFERENCES system.[User](Id) ON DELETE SET NULL;
		PRINT N'FK برای MechanicalUserId اضافه شد.';
	END

	IF NOT EXISTS (
		SELECT 1 FROM sys.foreign_keys
		WHERE name = 'FK_ProductionOrderEquipmentConfirmer_ElectricalUser'
	)
	BEGIN
		ALTER TABLE Pln.ProductionOrderEquipmentConfirmer
			ADD CONSTRAINT FK_ProductionOrderEquipmentConfirmer_ElectricalUser
			FOREIGN KEY (ElectricalUserId) REFERENCES system.[User](Id) ON DELETE SET NULL;
		PRINT N'FK برای ElectricalUserId اضافه شد.';
	END

	COMMIT TRANSACTION;
	PRINT N'--- پایان موفق FixProductionOrderEquipmentConfirmer ---';

END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;

	DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
	DECLARE @ErrSeverity INT = ERROR_SEVERITY();
	DECLARE @ErrState INT = ERROR_STATE();
	RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-----------------------------------------------------------------------------
-- تایید نتیجه
-----------------------------------------------------------------------------
SELECT
	c.name AS ColumnName,
	t.name AS DataType,
	c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('Pln.ProductionOrderEquipmentConfirmer')
ORDER BY c.column_id;

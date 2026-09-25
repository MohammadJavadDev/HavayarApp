-- ============================================
-- همگام‌سازی Bom از TotalSystem
-- فرمول (FormulGroup / Formul / FormulItem) دیگر در HavayarApp ذخیره نمی‌شود.
-- قطعات داخل فرمول هنگام سینک BOM محصول در SyncProductBomFromTotalSystem.sql
-- (و FormulaSyncJob) با ضریب ضرب‌شده روی Bom.ProductFormulItem می‌نشینند.
-- این فایل فقط به آن اسکریپت ارجاع می‌دهد تا فراخوانی‌های قدیمی نشکنند.
-- ============================================

SET NOCOUNT ON;

PRINT N'همگام‌سازی جداول فرمول منسوخ شده است.';
PRINT N'برای بازسازی BOM محصول تخت‌شده، SyncProductBomFromTotalSystem.sql را اجرا کنید.';
PRINT N'(یا جاب RebuildFormulasFromHts / FormulaSyncJob)';

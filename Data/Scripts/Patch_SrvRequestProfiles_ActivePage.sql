/*
Patch_SrvRequestProfiles_ActivePage.sql
دکمه‌های سفارشی نمایه‌های Srv_* (پیوست مدارک / بررسی و تایید) در scope سراسری اجرا می‌شوند و متغیر `page` ندارند؛
`typeof page` همیشه false بود و مودال باز نمی‌شد. صفحهٔ فعال از appController.activePage گرفته می‌شود.
Idempotent. اجرا: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -x -i "Data\Scripts\Patch_SrvRequestProfiles_ActivePage.sql"
*/
SET NOCOUNT ON;
DECLARE @OldGetter NVARCHAR(400) = N'var getter = (typeof page !== ''undefined'' && page.getPartialView) ? page.getPartialView.bind(page) : null;';
DECLARE @NewGetter NVARCHAR(400) = N'var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;';
DECLARE @OldFn NVARCHAR(200) = N'(page, (typeof $$ !== ''undefined'' ? $$ : jQuery), self)';
DECLARE @NewFn NVARCHAR(300) = N'(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self)';

UPDATE system.SavedQuery
SET CustomActionButtonsJson = REPLACE(REPLACE(CustomActionButtonsJson, @OldGetter, @NewGetter), @OldFn, @NewFn)
WHERE Name LIKE N'Srv[_]%' AND CustomActionButtonsJson LIKE N'%typeof page%';
PRINT N'Updated profiles: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT COUNT(*) AS StillUsingGlobalPage FROM system.SavedQuery WHERE CustomActionButtonsJson LIKE N'%typeof page%';
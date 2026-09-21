/*
================================================================================
Seed_OpenOrderRequest_NotificationGroups.sql  —  WP8
================================================================================
گروه‌های اعلان درخواست‌های باز. SupplyUnit و Industrial از قبل با HasData (Id 1 و 2)
وجود دارند؛ این اسکریپت آن‌ها را دست نمی‌زند و فقط گروه‌های غایب را درج می‌کند.

  Code                                         جایگزین HTS
  -------------------------------------------  --------------------------------------------------
  Sup.OpenOrderRequest.SupplyUnit              (موجود) کارتابل تدارکات / اسکالیشن 2821
  Sup.OpenOrderRequest.Industrial              (موجود) واحد صنایع
  Sup.OpenOrderRequest.ArrivalWarehouse        لیست ثابت رسید کامل انبار (ArrivalExtraEmails)
  Sup.OpenOrderRequest.ArrivalPartial          Saemian.r — رسید قسمتی
  Sup.OpenOrderRequest.QcRejection             لیست ثابت عدم تایید QC
  Sup.OpenOrderRequest.UnsentDispatchDigest    Dordab.y + CC پنج‌نفره «برگ ارسال نخورده»

Idempotent روی Code. اعضا: Seed_OpenOrderRequest_NotificationGroupMembers.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-notificationgroups';

DECLARE @Groups TABLE (Code NVARCHAR(100), DisplayName NVARCHAR(200), Description NVARCHAR(1500));
INSERT INTO @Groups (Code, DisplayName, Description) VALUES
 (N'Sup.OpenOrderRequest.SupplyUnit',
  N'تامین و خرید - درخواست های باز - واحد تدارکات',
  N'گروه تدارکات (اسکالیشن 2821، fallback دایجست برگ ارسال‌نخورده)'),
 (N'Sup.OpenOrderRequest.Industrial',
  N'تامین و خرید - درخواست های باز - واحد صنایع',
  N'گروه صنایع استفاده شده در OpenOrderRequestJob / کنترلر'),
 (N'Sup.OpenOrderRequest.ArrivalWarehouse',
  N'تامین و خرید - درخواست های باز - رسید کامل انبار',
  N'جایگزین ArrivalExtraEmails در OpenOrderRequestJob (HTS: Ahmadi.sh, Asadi.re, Moradmand.a, Azadbakhsh.*, zamani.z, Zandi.n, Eezi.a, khederzadeh.s, Saemian.r)'),
 (N'Sup.OpenOrderRequest.ArrivalPartial',
  N'تامین و خرید - درخواست های باز - رسید قسمتی',
  N'جایگزین ایمیل ثابت Saemian.r برای رسید ناقص'),
 (N'Sup.OpenOrderRequest.QcRejection',
  N'تامین و خرید - درخواست های باز - عدم تایید QC',
  N'جایگزین QcRejectionExtraEmails در OpenOrderRequestJob'),
 (N'Sup.OpenOrderRequest.UnsentDispatchDigest',
  N'تامین و خرید - درخواست های باز - دایجست برگ ارسال نخورده',
  N'HTS SendSupplyCommentNotifications: To=Dordab.y CC=Yaltaghian.f;Bagheri.h;Shahpordeli.s;Sohrabi.za;Sharifi.b');

INSERT INTO system.NotificationGroup
    (Code, DisplayName, Description,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT g.Code, g.DisplayName, g.Description,
       1, 1, @SeedUser, @SeedUser,
       @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM @Groups g
WHERE NOT EXISTS (SELECT 1 FROM system.NotificationGroup n WHERE n.Code = g.Code);

PRINT N'NotificationGroup rows inserted: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

SELECT n.Id, n.Code, n.DisplayName,
       (SELECT COUNT(*) FROM system.NotificationGroupMember m WHERE m.NotificationGroupId = n.Id AND m.IsActive = 1) AS Members
FROM system.NotificationGroup n
WHERE n.Code IN (SELECT Code FROM @Groups)
ORDER BY n.Code;

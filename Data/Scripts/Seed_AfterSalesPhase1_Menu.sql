/*
Append phase-1 leaf items to AfterSalesServiceSystem menu.
Run after Seed_AfterSalesServiceSystem_Menu.sql.
*/

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @MenuContent NVARCHAR(MAX) = N'[
  {"text":"مدیریت مناقصات","icon":"ki-briefcase ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[]},
  {"text":"فروش","icon":"ki-shop ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[{"text":"حواله‌های فروش","icon":"ki-delivery ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list","a_attr":{"href":"/panel/sale/orderdetail/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list"},"children":[]}]},
  {"text":"خدمات پس از فروش","icon":"ki-shield-tick ki-outline","iconColor":"#50dcae","path":"","a_attr":{"href":""},"data":{"iconColor":"#50dcae","path":""},"children":[{"text":"مانیتورینگ فروش","icon":"ki-chart-line ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/orderdetailserial/monitoring","a_attr":{"href":"/panel/sale/orderdetailserial/monitoring"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/orderdetailserial/monitoring"},"children":[]},{"text":"پیوست سریال","icon":"ki-paper-clip ki-outline","iconColor":"#1fe57f","path":"/panel/sale/orderdetailserial/list","a_attr":{"href":"/panel/sale/orderdetailserial/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/orderdetailserial/list"},"children":[]},{"text":"سایت‌های مشتریان","icon":"ki-geolocation ki-outline","iconColor":"#1fc4e5","path":"/panel/sls/customeraddress/list","a_attr":{"href":"/panel/sls/customeraddress/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sls/customeraddress/list"},"children":[]},{"text":"مناطق","icon":"ki-map ki-outline","iconColor":"#d78819","path":"/panel/crm/zone/list","a_attr":{"href":"/panel/crm/zone/list"},"data":{"iconColor":"#d78819","path":"/panel/crm/zone/list"},"children":[]},{"text":"اعزام کارشناس","icon":"ki-send ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/servicerequest/dispatch","a_attr":{"href":"/panel/sale/servicerequest/dispatch"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/servicerequest/dispatch"},"children":[]}]},
  {"text":"گزارشات","icon":"ki-chart-simple-3 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[]},
  {"text":"تعمیرات","icon":"ki-wrench ki-outline","iconColor":"#ed230c","path":"","a_attr":{"href":""},"data":{"iconColor":"#ed230c","path":""},"children":[]},
  {"text":"پرسش فنی","icon":"ki-questionnaire-tablet ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[]},
  {"text":"نمایندگی","icon":"ki-home ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[]},
  {"text":"نفت و گاز","icon":"ki-drop ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[]},
  {"text":"مدیریت کاربران موبایل","icon":"ki-phone ki-outline","iconColor":"#50dcae","path":"","a_attr":{"href":""},"data":{"iconColor":"#50dcae","path":""},"children":[]},
  {"text":"مانیتورینگ تجهیزات","icon":"ki-technology-4 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[]},
  {"text":"رضایت‌سنجی بعد از فروش","icon":"ki-like ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[]}
]';

UPDATE system.SystemMenu
SET Content = @MenuContent,
    ModifiedById = 1,
    ModifiedByName = N'seed-after-sales-phase1',
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Name = N'AfterSalesServiceSystem';

IF @@ROWCOUNT = 0
    PRINT N'WARN: AfterSalesServiceSystem menu missing — run Seed_AfterSalesServiceSystem_Menu.sql first';
ELSE
    PRINT N'Updated AfterSalesServiceSystem leaves for phase 1';

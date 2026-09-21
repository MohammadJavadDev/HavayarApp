/*
Seed_AfterSalesFull_Menu.sql — full leaf tree for AfterSalesServiceSystem.
Run after Seed_AfterSalesServiceSystem_Menu.sql. Idempotent on Name.
*/
SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @MenuContent NVARCHAR(MAX) = N'[
  {"text":"مدیریت مناقصات","icon":"ki-briefcase ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[
    {"text":"مناقصه","icon":"ki-briefcase ki-outline","iconColor":"#d78819","path":"/panel/sale/tendermanagement/list","a_attr":{"href":"/panel/sale/tendermanagement/list"},"data":{"iconColor":"#d78819","path":"/panel/sale/tendermanagement/list"},"children":[]}
  ]},
  {"text":"فروش","icon":"ki-shop ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[
    {"text":"تنظیمات قیمت","icon":"ki-setting-2 ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/priceconfig/list","a_attr":{"href":"/panel/sale/priceconfig/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/priceconfig/list"},"children":[]},
    {"text":"حواله‌های فروش","icon":"ki-delivery ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list","a_attr":{"href":"/panel/sale/orderdetail/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list"},"children":[]},
    {"text":"قیمت مصوب","icon":"ki-dollar ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/partprice/list","a_attr":{"href":"/panel/sale/partprice/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/partprice/list"},"children":[]},
    {"text":"سفارشات فروشگاه","icon":"ki-basket ki-outline","iconColor":"#50dcae","path":"/panel/sale/webshoporder/list","a_attr":{"href":"/panel/sale/webshoporder/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/webshoporder/list"},"children":[]},
    {"text":"محصولات فروشگاه","icon":"ki-some-files ki-outline","iconColor":"#1fe57f","path":"/panel/sale/webshoppartforsale/list","a_attr":{"href":"/panel/sale/webshoppartforsale/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/webshoppartforsale/list"},"children":[]}
  ]},
  {"text":"خدمات پس از فروش","icon":"ki-shield-tick ki-outline","iconColor":"#50dcae","path":"","a_attr":{"href":""},"data":{"iconColor":"#50dcae","path":""},"children":[
    {"text":"نقطه سفارش","icon":"ki-flag ki-outline","iconColor":"#d78819","path":"/panel/sale/orderpoint/list","a_attr":{"href":"/panel/sale/orderpoint/list"},"data":{"iconColor":"#d78819","path":"/panel/sale/orderpoint/list"},"children":[]},
    {"text":"وصول مطالبات","icon":"ki-wallet ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/collectionclaim/list","a_attr":{"href":"/panel/sale/collectionclaim/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/collectionclaim/list"},"children":[]},
    {"text":"اقلام مصرفی در پروژه","icon":"ki-cube-2 ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/projectutilizedmaterial/list","a_attr":{"href":"/panel/sale/projectutilizedmaterial/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/projectutilizedmaterial/list"},"children":[]},
    {"text":"حق ماموریت","icon":"ki-dollar ki-outline","iconColor":"#50dcae","path":"/panel/sale/missionsalary/list","a_attr":{"href":"/panel/sale/missionsalary/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/missionsalary/list"},"children":[]},
    {"text":"مدیریت درخواست پشتیبانی","icon":"ki-support ki-outline","iconColor":"#1fe57f","path":"/panel/sale/servicerequest/list","a_attr":{"href":"/panel/sale/servicerequest/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/servicerequest/list"},"children":[]},
    {"text":"اعزام کارشناس","icon":"ki-send ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/servicerequest/dispatch","a_attr":{"href":"/panel/sale/servicerequest/dispatch"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/servicerequest/dispatch"},"children":[]},
    {"text":"مانیتورینگ فروش","icon":"ki-chart-line ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/orderdetailserial/monitoring","a_attr":{"href":"/panel/sale/orderdetailserial/monitoring"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/orderdetailserial/monitoring"},"children":[]},
    {"text":"کارتابل مسئول منطقه","icon":"ki-tablet ki-outline","iconColor":"#50dcae","path":"/panel/sale/servicerequestdetail/cartable","a_attr":{"href":"/panel/sale/servicerequestdetail/cartable"},"data":{"iconColor":"#50dcae","path":"/panel/sale/servicerequestdetail/cartable"},"children":[]},
    {"text":"پیوست سریال","icon":"ki-paper-clip ki-outline","iconColor":"#1fe57f","path":"/panel/sale/orderdetailserial/list","a_attr":{"href":"/panel/sale/orderdetailserial/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/orderdetailserial/list"},"children":[]},
    {"text":"گارانتی مجاز مشتریان","icon":"ki-shield-tick ki-outline","iconColor":"#d78819","path":"/panel/sale/customerallowedguarantee/list","a_attr":{"href":"/panel/sale/customerallowedguarantee/list"},"data":{"iconColor":"#d78819","path":"/panel/sale/customerallowedguarantee/list"},"children":[]},
    {"text":"مسئولین مناطق","icon":"ki-geolocation ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/responsiblezone/list","a_attr":{"href":"/panel/sale/responsiblezone/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/responsiblezone/list"},"children":[]},
    {"text":"وصول مسئولین","icon":"ki-wallet ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/responsiblezonereceipt/list","a_attr":{"href":"/panel/sale/responsiblezonereceipt/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/responsiblezonereceipt/list"},"children":[]},
    {"text":"جزئیات سرویس دوره‌ای","icon":"ki-time ki-outline","iconColor":"#50dcae","path":"/panel/sale/productactivityitem/list","a_attr":{"href":"/panel/sale/productactivityitem/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/productactivityitem/list"},"children":[]},
    {"text":"اختصاص قطعه به محصول","icon":"ki-abstract-26 ki-outline","iconColor":"#1fe57f","path":"/panel/sale/orderdetailproduct/list","a_attr":{"href":"/panel/sale/orderdetailproduct/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/orderdetailproduct/list"},"children":[]},
    {"text":"سایت های مشتریان","icon":"ki-geolocation ki-outline","iconColor":"#1fc4e5","path":"/panel/sls/customeraddress/list","a_attr":{"href":"/panel/sls/customeraddress/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sls/customeraddress/list"},"children":[]},
    {"text":"درخواست مشتریان","icon":"ki-message-text ki-outline","iconColor":"#50dcae","path":"/panel/sale/customerrequest/list","a_attr":{"href":"/panel/sale/customerrequest/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/customerrequest/list"},"children":[]},
    {"text":"مناطق","icon":"ki-map ki-outline","iconColor":"#d78819","path":"/panel/crm/zone/list","a_attr":{"href":"/panel/crm/zone/list"},"data":{"iconColor":"#d78819","path":"/panel/crm/zone/list"},"children":[]}
  ]},
  {"text":"گزارشات","icon":"ki-chart-simple-3 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[
    {"text":"داشبورد خدمات پس از فروش","icon":"ki-chart-line ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/aftersalesdashboard/report","a_attr":{"href":"/panel/sale/aftersalesdashboard/report"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/aftersalesdashboard/report"},"children":[]},
    {"text":"گزارش کار ماموریت‌ها","icon":"ki-document ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/workreport/list","a_attr":{"href":"/panel/sale/workreport/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/workreport/list"},"children":[]}
  ]},
  {"text":"تعمیرات","icon":"ki-wrench ki-outline","iconColor":"#ed230c","path":"","a_attr":{"href":""},"data":{"iconColor":"#ed230c","path":""},"children":[
    {"text":"هزینه تقریبی","icon":"ki-dollar ki-outline","iconColor":"#ed230c","path":"/panel/rpr/estimatedcost/list","a_attr":{"href":"/panel/rpr/estimatedcost/list"},"data":{"iconColor":"#ed230c","path":"/panel/rpr/estimatedcost/list"},"children":[]},
    {"text":"درخواست تعمیر","icon":"ki-wrench ki-outline","iconColor":"#ed230c","path":"/panel/rpr/repairrequest/list","a_attr":{"href":"/panel/rpr/repairrequest/list"},"data":{"iconColor":"#ed230c","path":"/panel/rpr/repairrequest/list"},"children":[]},
    {"text":"سفارش پیمانکار","icon":"ki-handshake ki-outline","iconColor":"#d78819","path":"/panel/rpr/contractororder/list","a_attr":{"href":"/panel/rpr/contractororder/list"},"data":{"iconColor":"#d78819","path":"/panel/rpr/contractororder/list"},"children":[]},
    {"text":"تاخیرات جدولی","icon":"ki-tablet ki-outline","iconColor":"#e5ca1f","path":"/panel/rpr/repairrequest/delaylist","a_attr":{"href":"/panel/rpr/repairrequest/delaylist"},"data":{"iconColor":"#e5ca1f","path":"/panel/rpr/repairrequest/delaylist"},"children":[]},
    {"text":"تاخیرات نموداری","icon":"ki-chart-simple-3 ki-outline","iconColor":"#50dcae","path":"/panel/rpr/repairrequest/delaychart","a_attr":{"href":"/panel/rpr/repairrequest/delaychart"},"data":{"iconColor":"#50dcae","path":"/panel/rpr/repairrequest/delaychart"},"children":[]}
  ]},
  {"text":"پرسش فنی","icon":"ki-questionnaire-tablet ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[
    {"text":"TQ","icon":"ki-questionnaire-tablet ki-outline","iconColor":"#1fe57f","path":"/panel/sale/technicalquery/list","a_attr":{"href":"/panel/sale/technicalquery/list"},"data":{"iconColor":"#1fe57f","path":"/panel/sale/technicalquery/list"},"children":[]},
    {"text":"بررسی و تایید","icon":"ki-check ki-outline","iconColor":"#50dcae","path":"/panel/sale/technicalquery/approvequeue","a_attr":{"href":"/panel/sale/technicalquery/approvequeue"},"data":{"iconColor":"#50dcae","path":"/panel/sale/technicalquery/approvequeue"},"children":[]}
  ]},
  {"text":"نمایندگی","icon":"ki-home ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[
    {"text":"کاردکس قطعه","icon":"ki-book ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/agencypartcardex/list","a_attr":{"href":"/panel/sale/agencypartcardex/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/agencypartcardex/list"},"children":[]},
    {"text":"کارتابل نمایندگی","icon":"ki-tablet ki-outline","iconColor":"#50dcae","path":"/panel/sale/agencycartable/list","a_attr":{"href":"/panel/sale/agencycartable/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/agencycartable/list"},"children":[]}
  ]},
  {"text":"نفت و گاز","icon":"ki-drop ki-outline","iconColor":"#d78819","path":"","a_attr":{"href":""},"data":{"iconColor":"#d78819","path":""},"children":[
    {"text":"گزارش کار","icon":"ki-document ki-outline","iconColor":"#d78819","path":"/panel/sale/oilgasworkreport/list","a_attr":{"href":"/panel/sale/oilgasworkreport/list"},"data":{"iconColor":"#d78819","path":"/panel/sale/oilgasworkreport/list"},"children":[]}
  ]},
  {"text":"مدیریت کاربران موبایل","icon":"ki-phone ki-outline","iconColor":"#50dcae","path":"/panel/sale/aftersalesmobileuser/list","a_attr":{"href":"/panel/sale/aftersalesmobileuser/list"},"data":{"iconColor":"#50dcae","path":"/panel/sale/aftersalesmobileuser/list"},"children":[]},
  {"text":"مانیتورینگ تجهیزات","icon":"ki-technology-4 ki-outline","iconColor":"#e5ca1f","path":"/panel/sale/equipmentmonitoring/list","a_attr":{"href":"/panel/sale/equipmentmonitoring/list"},"data":{"iconColor":"#e5ca1f","path":"/panel/sale/equipmentmonitoring/list"},"children":[]},
  {"text":"رضایت‌سنجی بعد از فروش","icon":"ki-like ki-outline","iconColor":"#1fe57f","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fe57f","path":""},"children":[
    {"text":"بعد از فروش","icon":"ki-like ki-outline","iconColor":"#1fe57f","path":"/panel/crm/customersatisfactionsurvey/list","a_attr":{"href":"/panel/crm/customersatisfactionsurvey/list"},"data":{"iconColor":"#1fe57f","path":"/panel/crm/customersatisfactionsurvey/list"},"children":[]},
    {"text":"خدمات هوای فشرده","icon":"ki-like ki-outline","iconColor":"#1fc4e5","path":"/panel/crm/customersatisfactionsurvey/compressedairservices","a_attr":{"href":"/panel/crm/customersatisfactionsurvey/compressedairservices"},"data":{"iconColor":"#1fc4e5","path":"/panel/crm/customersatisfactionsurvey/compressedairservices"},"children":[]},
    {"text":"بعد از فروش CNG","icon":"ki-like ki-outline","iconColor":"#d78819","path":"/panel/crm/customersatisfactionsurvey/cngaftersales","a_attr":{"href":"/panel/crm/customersatisfactionsurvey/cngaftersales"},"data":{"iconColor":"#d78819","path":"/panel/crm/customersatisfactionsurvey/cngaftersales"},"children":[]}
  ]}
]';

UPDATE system.SystemMenu
SET Content = @MenuContent,
    ModifiedById = 1,
    ModifiedByName = N'seed-after-sales-full-menu',
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Name = N'AfterSalesServiceSystem';

IF @@ROWCOUNT = 0
    PRINT N'WARN: AfterSalesServiceSystem menu missing — run Seed_AfterSalesServiceSystem_Menu.sql first';
ELSE
    PRINT N'Updated AfterSalesServiceSystem full menu';

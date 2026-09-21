/*
  Seed_SaleOrderDetail_DataProfiles.sql
  گام چهارم: نمایه‌های داده (با قیمت / بدون قیمت) + RoleAccess DataProfile + منوی فروش
  ستون‌ها و ترتیب مطابق گرید HTS صفحه Sale_Order_Detail (_SaleOrderDetailManagement).
  ColumnName = Alliance (حالت Query).
  Idempotent — safe to re-run.
  Depends on roles from Seed_SaleOrderDetail_RolesAndUsers.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-orderdetail-dataprofiles';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.orderdetail';
    DECLARE @EntityFullNamePascal NVARCHAR(200) = N'Entities.App.Sale.OrderDetail';

    -----------------------------------------------------------------------------
    PRINT N'=== [1/4] Build ColumnsJson / CustomQuery / Action buttons ===';
    -----------------------------------------------------------------------------

    DECLARE @BaseSelectWithPrice NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[StatusEnum] AS [t2_StatusEnum],
    [t5].[Code] AS [t5_Code],
    [t6].[FullName] AS [t6_FullName],
    [t7].[Title] AS [t7_Title],
    [tStk].[Title] AS [tStk_Title],
    COALESCE([tProv].[Name], [tAddr].[ProvinceTitle]) AS [tAddr_ProvinceTitle],
    [tAddr].[ZoneTitle] AS [tAddr_ZoneTitle],
    [t2].[Year] AS [t2_Year],
    [t2].[VchDateShamsiDate] AS [t2_VchDateShamsiDate],
    [t2].[OrderNumber] AS [t2_OrderNumber],
    [t1].[Prefactor_VchNo] AS [t1_Prefactor_VchNo],
    [tTot].[TotalPrice] AS [tTot_TotalPrice],
    [t2].[VoucherTypeStatus] AS [t2_VoucherTypeStatus],
    [tBase].[Title] AS [tBase_Title],
    [t1].[Seq] AS [t1_Seq],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t3].[Type] AS [t3_Type],
    [t1].[Qty] AS [t1_Qty],
    [t1].[UnitPrice] AS [t1_UnitPrice],
    [t1].[Price] AS [t1_Price],
    [t1].[IncPrice] AS [t1_IncPrice],
    [t1].[DecPrice] AS [t1_DecPrice],
    [t1].[Description] AS [t1_Description]
FROM [Sale].[OrderDetail] AS [t1]
LEFT JOIN [Sale].[Order] AS [t2] ON [t1].[Sale_OrderId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[PartId] = [t3].[Id]
LEFT JOIN [SLS].[Customer] AS [t5] ON COALESCE([t1].[CustomerId], [t2].[CustomerId]) = [t5].[Id]
LEFT JOIN [Gnr].[Party] AS [t6] ON [t5].[PartyId] = [t6].[Id]
LEFT JOIN [Sale].[Branch] AS [t7] ON [t2].[BranchId] = [t7].[Id]
LEFT JOIN [Gnr].[Region] AS [tProv] ON [t5].[ProvinceId] = [tProv].[Id]
LEFT JOIN (
    SELECT CAST(1 AS bigint) AS [Id], N''N''''انبار خدمات HY'''')'' AS [Title]
    UNION ALL SELECT CAST(2 AS bigint) AS [Id], N''N''''انبار  CKD  اطلس'''')'' AS [Title]
    UNION ALL SELECT CAST(3 AS bigint) AS [Id], N''N''''انبار خدمات و پیمانکاری اطلس'''')'' AS [Title]
    UNION ALL SELECT CAST(4 AS bigint) AS [Id], N''N''''انبار مواد و قطعات هوایار'''')'' AS [Title]
    UNION ALL SELECT CAST(5 AS bigint) AS [Id], N''N''''انبار ابزار و ملزومات'''')'' AS [Title]
    UNION ALL SELECT CAST(6 AS bigint) AS [Id], N''N''''انبار محصولات'''')'' AS [Title]
    UNION ALL SELECT CAST(7 AS bigint) AS [Id], N''N''''انبار سامسونگ'''')'' AS [Title]
    UNION ALL SELECT CAST(8 AS bigint) AS [Id], N''N''''انبار ضایعات'''')'' AS [Title]
    UNION ALL SELECT CAST(9 AS bigint) AS [Id], N''N''''انبار کالای در جریان'''')'' AS [Title]
    UNION ALL SELECT CAST(10 AS bigint) AS [Id], N''N''''انبار قابل تعمیر قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(11 AS bigint) AS [Id], N''N''''انبار تعمیراتی قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(17 AS bigint) AS [Id], N''N''''انبار خدمات CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(18 AS bigint) AS [Id], N''N''''انبار قابل تعمیر CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(22 AS bigint) AS [Id], N''N''''انبار تعمیراتی CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(27 AS bigint) AS [Id], N''N''''انبار مواد و قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(28 AS bigint) AS [Id], N''N''''انبار تعمیراتی سالم'''')'' AS [Title]
    UNION ALL SELECT CAST(29 AS bigint) AS [Id], N''N''''انبار قابل تعمیر محصول'''')'' AS [Title]
    UNION ALL SELECT CAST(30 AS bigint) AS [Id], N''N''''انبار تعمیراتی محصول'''')'' AS [Title]
    UNION ALL SELECT CAST(31 AS bigint) AS [Id], N''N''''انبار راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(32 AS bigint) AS [Id], N''N''''انبار قابل تعمیر راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(33 AS bigint) AS [Id], N''N''''انبار تعمیراتی راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(34 AS bigint) AS [Id], N''N''''انبار قابل تعمیر داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(35 AS bigint) AS [Id], N''N''''انبار تعمیراتی داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(36 AS bigint) AS [Id], N''N''''انبار تعمیراتی سالم داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(37 AS bigint) AS [Id], N''N''''انبار تدارکات'''')'' AS [Title]
    UNION ALL SELECT CAST(39 AS bigint) AS [Id], N''N''''انبار پروژه'''')'' AS [Title]
    UNION ALL SELECT CAST(42 AS bigint) AS [Id], N''N''''انبار سوخت'''')'' AS [Title]
    UNION ALL SELECT CAST(43 AS bigint) AS [Id], N''N''''انبار راکد داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(44 AS bigint) AS [Id], N''N''''انبار واحد تعمیرات 2'''')'' AS [Title]
    UNION ALL SELECT CAST(45 AS bigint) AS [Id], N''N''''انبار مرجوعی تدارکات'''')'' AS [Title]
    UNION ALL SELECT CAST(46 AS bigint) AS [Id], N''N''''انبار راکد سامسونگ'''')'' AS [Title]
    UNION ALL SELECT CAST(47 AS bigint) AS [Id], N''N''''پروژه سروش مهستان عسلویه - 7530'''')'' AS [Title]
    UNION ALL SELECT CAST(48 AS bigint) AS [Id], N''N''''پروژه تبدیل انرژی پایا - 7679'''')'' AS [Title]
    UNION ALL SELECT CAST(50 AS bigint) AS [Id], N''N''''پروژه فرا دست انرژی فلات - 7674'''')'' AS [Title]
    UNION ALL SELECT CAST(51 AS bigint) AS [Id], N''N''''پروژه NGL 3100 AIR - س س 6508'''')'' AS [Title]
    UNION ALL SELECT CAST(52 AS bigint) AS [Id], N''N''''پروژه NGL 3100 N2 - س س 6488'''')'' AS [Title]
    UNION ALL SELECT CAST(53 AS bigint) AS [Id], N''N''''پروژه گچساران'''')'' AS [Title]
    UNION ALL SELECT CAST(54 AS bigint) AS [Id], N''N''''پروژه NGL 3200'''')'' AS [Title]
    UNION ALL SELECT CAST(56 AS bigint) AS [Id], N''N''''پالایش گاز بید بلند خلیج فارس-8121-7611'''')'' AS [Title]
    UNION ALL SELECT CAST(58 AS bigint) AS [Id], N''N''''پروژه فولاد کویر یزد -7946 و 7493'''')'' AS [Title]
    UNION ALL SELECT CAST(59 AS bigint) AS [Id], N''N''''پروژه کنگان - پتروپالایش کنگان - 7871'''')'' AS [Title]
    UNION ALL SELECT CAST(61 AS bigint) AS [Id], N''N''''پروژه نفت سپاهان - 7625'''')'' AS [Title]
    UNION ALL SELECT CAST(63 AS bigint) AS [Id], N''N''''پروژه دیکوک'''')'' AS [Title]
    UNION ALL SELECT CAST(64 AS bigint) AS [Id], N''N''''صنایع پتروشیمی پروژه دره زار-7751'''')'' AS [Title]
    UNION ALL SELECT CAST(65 AS bigint) AS [Id], N''N''''پروژه جندی شاپور شیراز - 7421'''')'' AS [Title]
    UNION ALL SELECT CAST(66 AS bigint) AS [Id], N''N''''پروژه سکاف - 7877-8045'''')'' AS [Title]
    UNION ALL SELECT CAST(68 AS bigint) AS [Id], N''N''''پروژه پالایشگاه آبادان - 7066'''')'' AS [Title]
    UNION ALL SELECT CAST(69 AS bigint) AS [Id], N''N''''پروژه ساختمانی انهار - 7936-7948'''')'' AS [Title]
    UNION ALL SELECT CAST(71 AS bigint) AS [Id], N''N''''پروژه پالایشگاه تبریز-6532'''')'' AS [Title]
    UNION ALL SELECT CAST(72 AS bigint) AS [Id], N''N''''پروژه پالایشگاه شیراز - 7452-7495 -ODCC'''')'' AS [Title]
    UNION ALL SELECT CAST(74 AS bigint) AS [Id], N''N''''پروژه پتروشیمی کرمانشاه - 7178'''')'' AS [Title]
    UNION ALL SELECT CAST(75 AS bigint) AS [Id], N''N''''پروژه پتروشیمی هنگام - 7225'''')'' AS [Title]
    UNION ALL SELECT CAST(76 AS bigint) AS [Id], N''N''''مهندسی وساختمانی جهان پارس-نفت آذر 7416'''')'' AS [Title]
    UNION ALL SELECT CAST(77 AS bigint) AS [Id], N''N''''پروژه فولاد غرب آسیا - 7904'''')'' AS [Title]
    UNION ALL SELECT CAST(78 AS bigint) AS [Id], N''N''''پروژه توربوکمپرسورنفت-OTCC -7296-7297'''')'' AS [Title]
    UNION ALL SELECT CAST(80 AS bigint) AS [Id], N''N''''پروژه فکورصنعت تهران'''')'' AS [Title]
    UNION ALL SELECT CAST(84 AS bigint) AS [Id], N''N''''انبار پتروتجهیز نیکان'''')'' AS [Title]
    UNION ALL SELECT CAST(85 AS bigint) AS [Id], N''N''''پروژه نفت وگاز پارس- 7913'''')'' AS [Title]
    UNION ALL SELECT CAST(86 AS bigint) AS [Id], N''N''''پروژه لاوان - 7814'''')'' AS [Title]
    UNION ALL SELECT CAST(88 AS bigint) AS [Id], N''N''''دارایی ثابت'''')'' AS [Title]
    UNION ALL SELECT CAST(90 AS bigint) AS [Id], N''N''''پروژه طراحی و مهندسی صنایع انرژی-8000'''')'' AS [Title]
    UNION ALL SELECT CAST(91 AS bigint) AS [Id], N''N''''پروژه فنی مهندسی پتروشیمی فارس -8242'''')'' AS [Title]
    UNION ALL SELECT CAST(93 AS bigint) AS [Id], N''N''''پروژه توسعه صنایع نفت وانرژی قشم-8289'''')'' AS [Title]
    UNION ALL SELECT CAST(95 AS bigint) AS [Id], N''N''''پروژه لردگان'''')'' AS [Title]
    UNION ALL SELECT CAST(97 AS bigint) AS [Id], N''N''''آسکوتک هلدینگ (اپال پارسیان سنگان) - 8213 -8592'''')'' AS [Title]
    UNION ALL SELECT CAST(98 AS bigint) AS [Id], N''N''''پروژه سرمایه گذاری مس سرچشمه (خاتون آباد) - 8386'''')'' AS [Title]
    UNION ALL SELECT CAST(101 AS bigint) AS [Id], N''N''''پروژه آسفالت طوس -8379'''')'' AS [Title]
    UNION ALL SELECT CAST(102 AS bigint) AS [Id], N''N''''پروژه انرژی گستر نصیر – 8444'''')'' AS [Title]
    UNION ALL SELECT CAST(104 AS bigint) AS [Id], N''N''''راکدخدمات'''')'' AS [Title]
    UNION ALL SELECT CAST(105 AS bigint) AS [Id], N''N''''انبار فروش مزایده'''')'' AS [Title]
    UNION ALL SELECT CAST(106 AS bigint) AS [Id], N''N''''انبار فروش مزایده داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(108 AS bigint) AS [Id], N''N''''پروژه هیرگان انرژی (بینک)-7974'''')'' AS [Title]
    UNION ALL SELECT CAST(109 AS bigint) AS [Id], N''N''''پالایش گاز بید بلند خلیج فارس-جدید س س 8275'''')'' AS [Title]
    UNION ALL SELECT CAST(110 AS bigint) AS [Id], N''N''''پروژه بین المللی پتروتکسان-8482'''')'' AS [Title]
    UNION ALL SELECT CAST(112 AS bigint) AS [Id], N''N''''پروژه عمومی مازاد پروژه ها'''')'' AS [Title]
    UNION ALL SELECT CAST(116 AS bigint) AS [Id], N''N''''پروژه پالایشگاه نفت شیراز -7867 جدید'''')'' AS [Title]
    UNION ALL SELECT CAST(117 AS bigint) AS [Id], N''N''''پروژه پالایش نفت آفتاب -8535'''')'' AS [Title]
    UNION ALL SELECT CAST(124 AS bigint) AS [Id], N''N''''پروژه مهندسین مشاور چگالش -7761'''')'' AS [Title]
    UNION ALL SELECT CAST(125 AS bigint) AS [Id], N''N''''پروژه ملی حفاری ایران-8686'''')'' AS [Title]
    UNION ALL SELECT CAST(133 AS bigint) AS [Id], N''N''''پروژه سرمایه گذاری صنایع شیمیایی ایران-7940'''')'' AS [Title]
    UNION ALL SELECT CAST(134 AS bigint) AS [Id], N''N''''پروژه فولاد آلیاژی مرکزی بافق-8977'''')'' AS [Title]
    UNION ALL SELECT CAST(135 AS bigint) AS [Id], N''N''''پروژه توسعه معادن پارس تامین-9004'''')'' AS [Title]
    UNION ALL SELECT CAST(138 AS bigint) AS [Id], N''N''''خرید خارجی DENAIR'''')'' AS [Title]
) AS [tStk] ON [t2].[HtsStockId] = [tStk].[Id]
LEFT JOIN (
    SELECT CAST(100 AS bigint) AS [Id], N''از ابتدا'' AS [Title]
    UNION ALL SELECT 101, N''قرارداد''
    UNION ALL SELECT 102, N''پیش فاکتور''
) AS [tBase] ON [t2].[HtsBaseVoucherTypeId] = [tBase].[Id]
LEFT JOIN (
    SELECT [Sale_OrderId], SUM(CAST([Price] AS bigint)) AS [TotalPrice]
    FROM [Sale].[OrderDetail]
    GROUP BY [Sale_OrderId]
) AS [tTot] ON [t2].[Id] = [tTot].[Sale_OrderId]
OUTER APPLY (
    SELECT TOP 1
        [z].[Title] AS [ZoneTitle],
        [rp].[Name] AS [ProvinceTitle]
    FROM [SLS].[CustomerAddress] AS [ca]
    LEFT JOIN [Crm].[Zone] AS [z] ON [z].[Id] = [ca].[ZoneId]
    LEFT JOIN [Gnr].[Region] AS [rp] ON [rp].[Id] = [ca].[ProvinceId]
    WHERE [ca].[CustomerId] = [t5].[Id]
    ORDER BY CASE WHEN [ca].[ZoneId] IS NOT NULL THEN 0 ELSE 1 END, [ca].[Id]
) AS [tAddr]';

    DECLARE @BaseSelectNoPrice NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[StatusEnum] AS [t2_StatusEnum],
    [t5].[Code] AS [t5_Code],
    [t6].[FullName] AS [t6_FullName],
    [t7].[Title] AS [t7_Title],
    [tStk].[Title] AS [tStk_Title],
    COALESCE([tProv].[Name], [tAddr].[ProvinceTitle]) AS [tAddr_ProvinceTitle],
    [tAddr].[ZoneTitle] AS [tAddr_ZoneTitle],
    [t2].[Year] AS [t2_Year],
    [t2].[VchDateShamsiDate] AS [t2_VchDateShamsiDate],
    [t2].[OrderNumber] AS [t2_OrderNumber],
    [t1].[Prefactor_VchNo] AS [t1_Prefactor_VchNo],
    [t2].[VoucherTypeStatus] AS [t2_VoucherTypeStatus],
    [tBase].[Title] AS [tBase_Title],
    [t1].[Seq] AS [t1_Seq],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t3].[Type] AS [t3_Type],
    [t1].[Qty] AS [t1_Qty],
    [t1].[Description] AS [t1_Description]
FROM [Sale].[OrderDetail] AS [t1]
LEFT JOIN [Sale].[Order] AS [t2] ON [t1].[Sale_OrderId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[PartId] = [t3].[Id]
LEFT JOIN [SLS].[Customer] AS [t5] ON COALESCE([t1].[CustomerId], [t2].[CustomerId]) = [t5].[Id]
LEFT JOIN [Gnr].[Party] AS [t6] ON [t5].[PartyId] = [t6].[Id]
LEFT JOIN [Sale].[Branch] AS [t7] ON [t2].[BranchId] = [t7].[Id]
LEFT JOIN [Gnr].[Region] AS [tProv] ON [t5].[ProvinceId] = [tProv].[Id]
LEFT JOIN (
    SELECT CAST(1 AS bigint) AS [Id], N''N''''انبار خدمات HY'''')'' AS [Title]
    UNION ALL SELECT CAST(2 AS bigint) AS [Id], N''N''''انبار  CKD  اطلس'''')'' AS [Title]
    UNION ALL SELECT CAST(3 AS bigint) AS [Id], N''N''''انبار خدمات و پیمانکاری اطلس'''')'' AS [Title]
    UNION ALL SELECT CAST(4 AS bigint) AS [Id], N''N''''انبار مواد و قطعات هوایار'''')'' AS [Title]
    UNION ALL SELECT CAST(5 AS bigint) AS [Id], N''N''''انبار ابزار و ملزومات'''')'' AS [Title]
    UNION ALL SELECT CAST(6 AS bigint) AS [Id], N''N''''انبار محصولات'''')'' AS [Title]
    UNION ALL SELECT CAST(7 AS bigint) AS [Id], N''N''''انبار سامسونگ'''')'' AS [Title]
    UNION ALL SELECT CAST(8 AS bigint) AS [Id], N''N''''انبار ضایعات'''')'' AS [Title]
    UNION ALL SELECT CAST(9 AS bigint) AS [Id], N''N''''انبار کالای در جریان'''')'' AS [Title]
    UNION ALL SELECT CAST(10 AS bigint) AS [Id], N''N''''انبار قابل تعمیر قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(11 AS bigint) AS [Id], N''N''''انبار تعمیراتی قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(17 AS bigint) AS [Id], N''N''''انبار خدمات CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(18 AS bigint) AS [Id], N''N''''انبار قابل تعمیر CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(22 AS bigint) AS [Id], N''N''''انبار تعمیراتی CNG'''')'' AS [Title]
    UNION ALL SELECT CAST(27 AS bigint) AS [Id], N''N''''انبار مواد و قطعات'''')'' AS [Title]
    UNION ALL SELECT CAST(28 AS bigint) AS [Id], N''N''''انبار تعمیراتی سالم'''')'' AS [Title]
    UNION ALL SELECT CAST(29 AS bigint) AS [Id], N''N''''انبار قابل تعمیر محصول'''')'' AS [Title]
    UNION ALL SELECT CAST(30 AS bigint) AS [Id], N''N''''انبار تعمیراتی محصول'''')'' AS [Title]
    UNION ALL SELECT CAST(31 AS bigint) AS [Id], N''N''''انبار راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(32 AS bigint) AS [Id], N''N''''انبار قابل تعمیر راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(33 AS bigint) AS [Id], N''N''''انبار تعمیراتی راکد'''')'' AS [Title]
    UNION ALL SELECT CAST(34 AS bigint) AS [Id], N''N''''انبار قابل تعمیر داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(35 AS bigint) AS [Id], N''N''''انبار تعمیراتی داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(36 AS bigint) AS [Id], N''N''''انبار تعمیراتی سالم داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(37 AS bigint) AS [Id], N''N''''انبار تدارکات'''')'' AS [Title]
    UNION ALL SELECT CAST(39 AS bigint) AS [Id], N''N''''انبار پروژه'''')'' AS [Title]
    UNION ALL SELECT CAST(42 AS bigint) AS [Id], N''N''''انبار سوخت'''')'' AS [Title]
    UNION ALL SELECT CAST(43 AS bigint) AS [Id], N''N''''انبار راکد داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(44 AS bigint) AS [Id], N''N''''انبار واحد تعمیرات 2'''')'' AS [Title]
    UNION ALL SELECT CAST(45 AS bigint) AS [Id], N''N''''انبار مرجوعی تدارکات'''')'' AS [Title]
    UNION ALL SELECT CAST(46 AS bigint) AS [Id], N''N''''انبار راکد سامسونگ'''')'' AS [Title]
    UNION ALL SELECT CAST(47 AS bigint) AS [Id], N''N''''پروژه سروش مهستان عسلویه - 7530'''')'' AS [Title]
    UNION ALL SELECT CAST(48 AS bigint) AS [Id], N''N''''پروژه تبدیل انرژی پایا - 7679'''')'' AS [Title]
    UNION ALL SELECT CAST(50 AS bigint) AS [Id], N''N''''پروژه فرا دست انرژی فلات - 7674'''')'' AS [Title]
    UNION ALL SELECT CAST(51 AS bigint) AS [Id], N''N''''پروژه NGL 3100 AIR - س س 6508'''')'' AS [Title]
    UNION ALL SELECT CAST(52 AS bigint) AS [Id], N''N''''پروژه NGL 3100 N2 - س س 6488'''')'' AS [Title]
    UNION ALL SELECT CAST(53 AS bigint) AS [Id], N''N''''پروژه گچساران'''')'' AS [Title]
    UNION ALL SELECT CAST(54 AS bigint) AS [Id], N''N''''پروژه NGL 3200'''')'' AS [Title]
    UNION ALL SELECT CAST(56 AS bigint) AS [Id], N''N''''پالایش گاز بید بلند خلیج فارس-8121-7611'''')'' AS [Title]
    UNION ALL SELECT CAST(58 AS bigint) AS [Id], N''N''''پروژه فولاد کویر یزد -7946 و 7493'''')'' AS [Title]
    UNION ALL SELECT CAST(59 AS bigint) AS [Id], N''N''''پروژه کنگان - پتروپالایش کنگان - 7871'''')'' AS [Title]
    UNION ALL SELECT CAST(61 AS bigint) AS [Id], N''N''''پروژه نفت سپاهان - 7625'''')'' AS [Title]
    UNION ALL SELECT CAST(63 AS bigint) AS [Id], N''N''''پروژه دیکوک'''')'' AS [Title]
    UNION ALL SELECT CAST(64 AS bigint) AS [Id], N''N''''صنایع پتروشیمی پروژه دره زار-7751'''')'' AS [Title]
    UNION ALL SELECT CAST(65 AS bigint) AS [Id], N''N''''پروژه جندی شاپور شیراز - 7421'''')'' AS [Title]
    UNION ALL SELECT CAST(66 AS bigint) AS [Id], N''N''''پروژه سکاف - 7877-8045'''')'' AS [Title]
    UNION ALL SELECT CAST(68 AS bigint) AS [Id], N''N''''پروژه پالایشگاه آبادان - 7066'''')'' AS [Title]
    UNION ALL SELECT CAST(69 AS bigint) AS [Id], N''N''''پروژه ساختمانی انهار - 7936-7948'''')'' AS [Title]
    UNION ALL SELECT CAST(71 AS bigint) AS [Id], N''N''''پروژه پالایشگاه تبریز-6532'''')'' AS [Title]
    UNION ALL SELECT CAST(72 AS bigint) AS [Id], N''N''''پروژه پالایشگاه شیراز - 7452-7495 -ODCC'''')'' AS [Title]
    UNION ALL SELECT CAST(74 AS bigint) AS [Id], N''N''''پروژه پتروشیمی کرمانشاه - 7178'''')'' AS [Title]
    UNION ALL SELECT CAST(75 AS bigint) AS [Id], N''N''''پروژه پتروشیمی هنگام - 7225'''')'' AS [Title]
    UNION ALL SELECT CAST(76 AS bigint) AS [Id], N''N''''مهندسی وساختمانی جهان پارس-نفت آذر 7416'''')'' AS [Title]
    UNION ALL SELECT CAST(77 AS bigint) AS [Id], N''N''''پروژه فولاد غرب آسیا - 7904'''')'' AS [Title]
    UNION ALL SELECT CAST(78 AS bigint) AS [Id], N''N''''پروژه توربوکمپرسورنفت-OTCC -7296-7297'''')'' AS [Title]
    UNION ALL SELECT CAST(80 AS bigint) AS [Id], N''N''''پروژه فکورصنعت تهران'''')'' AS [Title]
    UNION ALL SELECT CAST(84 AS bigint) AS [Id], N''N''''انبار پتروتجهیز نیکان'''')'' AS [Title]
    UNION ALL SELECT CAST(85 AS bigint) AS [Id], N''N''''پروژه نفت وگاز پارس- 7913'''')'' AS [Title]
    UNION ALL SELECT CAST(86 AS bigint) AS [Id], N''N''''پروژه لاوان - 7814'''')'' AS [Title]
    UNION ALL SELECT CAST(88 AS bigint) AS [Id], N''N''''دارایی ثابت'''')'' AS [Title]
    UNION ALL SELECT CAST(90 AS bigint) AS [Id], N''N''''پروژه طراحی و مهندسی صنایع انرژی-8000'''')'' AS [Title]
    UNION ALL SELECT CAST(91 AS bigint) AS [Id], N''N''''پروژه فنی مهندسی پتروشیمی فارس -8242'''')'' AS [Title]
    UNION ALL SELECT CAST(93 AS bigint) AS [Id], N''N''''پروژه توسعه صنایع نفت وانرژی قشم-8289'''')'' AS [Title]
    UNION ALL SELECT CAST(95 AS bigint) AS [Id], N''N''''پروژه لردگان'''')'' AS [Title]
    UNION ALL SELECT CAST(97 AS bigint) AS [Id], N''N''''آسکوتک هلدینگ (اپال پارسیان سنگان) - 8213 -8592'''')'' AS [Title]
    UNION ALL SELECT CAST(98 AS bigint) AS [Id], N''N''''پروژه سرمایه گذاری مس سرچشمه (خاتون آباد) - 8386'''')'' AS [Title]
    UNION ALL SELECT CAST(101 AS bigint) AS [Id], N''N''''پروژه آسفالت طوس -8379'''')'' AS [Title]
    UNION ALL SELECT CAST(102 AS bigint) AS [Id], N''N''''پروژه انرژی گستر نصیر – 8444'''')'' AS [Title]
    UNION ALL SELECT CAST(104 AS bigint) AS [Id], N''N''''راکدخدمات'''')'' AS [Title]
    UNION ALL SELECT CAST(105 AS bigint) AS [Id], N''N''''انبار فروش مزایده'''')'' AS [Title]
    UNION ALL SELECT CAST(106 AS bigint) AS [Id], N''N''''انبار فروش مزایده داغی'''')'' AS [Title]
    UNION ALL SELECT CAST(108 AS bigint) AS [Id], N''N''''پروژه هیرگان انرژی (بینک)-7974'''')'' AS [Title]
    UNION ALL SELECT CAST(109 AS bigint) AS [Id], N''N''''پالایش گاز بید بلند خلیج فارس-جدید س س 8275'''')'' AS [Title]
    UNION ALL SELECT CAST(110 AS bigint) AS [Id], N''N''''پروژه بین المللی پتروتکسان-8482'''')'' AS [Title]
    UNION ALL SELECT CAST(112 AS bigint) AS [Id], N''N''''پروژه عمومی مازاد پروژه ها'''')'' AS [Title]
    UNION ALL SELECT CAST(116 AS bigint) AS [Id], N''N''''پروژه پالایشگاه نفت شیراز -7867 جدید'''')'' AS [Title]
    UNION ALL SELECT CAST(117 AS bigint) AS [Id], N''N''''پروژه پالایش نفت آفتاب -8535'''')'' AS [Title]
    UNION ALL SELECT CAST(124 AS bigint) AS [Id], N''N''''پروژه مهندسین مشاور چگالش -7761'''')'' AS [Title]
    UNION ALL SELECT CAST(125 AS bigint) AS [Id], N''N''''پروژه ملی حفاری ایران-8686'''')'' AS [Title]
    UNION ALL SELECT CAST(133 AS bigint) AS [Id], N''N''''پروژه سرمایه گذاری صنایع شیمیایی ایران-7940'''')'' AS [Title]
    UNION ALL SELECT CAST(134 AS bigint) AS [Id], N''N''''پروژه فولاد آلیاژی مرکزی بافق-8977'''')'' AS [Title]
    UNION ALL SELECT CAST(135 AS bigint) AS [Id], N''N''''پروژه توسعه معادن پارس تامین-9004'''')'' AS [Title]
    UNION ALL SELECT CAST(138 AS bigint) AS [Id], N''N''''خرید خارجی DENAIR'''')'' AS [Title]
) AS [tStk] ON [t2].[HtsStockId] = [tStk].[Id]
LEFT JOIN (
    SELECT CAST(100 AS bigint) AS [Id], N''از ابتدا'' AS [Title]
    UNION ALL SELECT 101, N''قرارداد''
    UNION ALL SELECT 102, N''پیش فاکتور''
) AS [tBase] ON [t2].[HtsBaseVoucherTypeId] = [tBase].[Id]
LEFT JOIN (
    SELECT [Sale_OrderId], SUM(CAST([Price] AS bigint)) AS [TotalPrice]
    FROM [Sale].[OrderDetail]
    GROUP BY [Sale_OrderId]
) AS [tTot] ON [t2].[Id] = [tTot].[Sale_OrderId]
OUTER APPLY (
    SELECT TOP 1
        [z].[Title] AS [ZoneTitle],
        [rp].[Name] AS [ProvinceTitle]
    FROM [SLS].[CustomerAddress] AS [ca]
    LEFT JOIN [Crm].[Zone] AS [z] ON [z].[Id] = [ca].[ZoneId]
    LEFT JOIN [Gnr].[Region] AS [rp] ON [rp].[Id] = [ca].[ProvinceId]
    WHERE [ca].[CustomerId] = [t5].[Id]
    ORDER BY CASE WHEN [ca].[ZoneId] IS NOT NULL THEN 0 ELSE 1 END, [ca].[Id]
) AS [tAddr]';

    DECLARE @MoneyRender NVARCHAR(300) = N'function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }';

    DECLARE @ColumnsWithPrice NVARCHAR(MAX) = N'[{"TableName":"Sale.OrderDetail","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_StatusEnum","DisplayName":"وضعیت","Alliance":"t2_StatusEnum","Address":"[t2].[StatusEnum]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.SaleOrderStatusEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t5_Code","DisplayName":"کد مشتری","Alliance":"t5_Code","Address":"[t5].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t6_FullName","DisplayName":"مشتری","Alliance":"t6_FullName","Address":"[t6].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Branch","ColumnName":"t7_Title","DisplayName":"مرکز فروش","Alliance":"t7_Title","Address":"[t7].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tStk_Title","DisplayName":"انبار","Alliance":"tStk_Title","Address":"[tStk].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"tAddr_ProvinceTitle","DisplayName":"استان","Alliance":"tAddr_ProvinceTitle","Address":"COALESCE([tProv].[Name], [tAddr].[ProvinceTitle])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"tAddr_ZoneTitle","DisplayName":"منطقه","Alliance":"tAddr_ZoneTitle","Address":"[tAddr].[ZoneTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_Year","DisplayName":"سال","Alliance":"t2_Year","Address":"[t2].[Year]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_VchDateShamsiDate","DisplayName":"تاریخ","Alliance":"t2_VchDateShamsiDate","Address":"[t2].[VchDateShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_OrderNumber","DisplayName":"شماره سند","Alliance":"t2_OrderNumber","Address":"[t2].[OrderNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Prefactor_VchNo","DisplayName":"ش پیش فاکتور","Alliance":"t1_Prefactor_VchNo","Address":"[t1].[Prefactor_VchNo]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"tTot_TotalPrice","DisplayName":"مجموع قیمت","Alliance":"tTot_TotalPrice","Address":"[tTot].[TotalPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_VoucherTypeStatus","DisplayName":"نوع سند","Alliance":"t2_VoucherTypeStatus","Address":"[t2].[VoucherTypeStatus]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.SaleOrderTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tBase_Title","DisplayName":"مبنای حواله","Alliance":"tBase_Title","Address":"[tBase].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Seq","DisplayName":"ردیف","Alliance":"t1_Seq","Address":"[t1].[Seq]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Type","DisplayName":"نوع","Alliance":"t3_Type","Address":"[t3].[Type]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Qty","DisplayName":"مقدار","Alliance":"t1_Qty","Address":"[t1].[Qty]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_UnitPrice","DisplayName":"قیمت واحد","Alliance":"t1_UnitPrice","Address":"[t1].[UnitPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Price","DisplayName":"مبلغ","Alliance":"t1_Price","Address":"[t1].[Price]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_IncPrice","DisplayName":"مجموع افزاینده","Alliance":"t1_IncPrice","Address":"[t1].[IncPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_DecPrice","DisplayName":"مجموع کاهنده","Alliance":"t1_DecPrice","Address":"[t1].[DecPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Description","DisplayName":"توضیحات","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""}]';

    DECLARE @ColumnsNoPrice NVARCHAR(MAX) = N'[{"TableName":"Sale.OrderDetail","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_StatusEnum","DisplayName":"وضعیت","Alliance":"t2_StatusEnum","Address":"[t2].[StatusEnum]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.SaleOrderStatusEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"t5_Code","DisplayName":"کد مشتری","Alliance":"t5_Code","Address":"[t5].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"t6_FullName","DisplayName":"مشتری","Alliance":"t6_FullName","Address":"[t6].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Branch","ColumnName":"t7_Title","DisplayName":"مرکز فروش","Alliance":"t7_Title","Address":"[t7].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tStk_Title","DisplayName":"انبار","Alliance":"tStk_Title","Address":"[tStk].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"tAddr_ProvinceTitle","DisplayName":"استان","Alliance":"tAddr_ProvinceTitle","Address":"COALESCE([tProv].[Name], [tAddr].[ProvinceTitle])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"tAddr_ZoneTitle","DisplayName":"منطقه","Alliance":"tAddr_ZoneTitle","Address":"[tAddr].[ZoneTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_Year","DisplayName":"سال","Alliance":"t2_Year","Address":"[t2].[Year]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_VchDateShamsiDate","DisplayName":"تاریخ","Alliance":"t2_VchDateShamsiDate","Address":"[t2].[VchDateShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_OrderNumber","DisplayName":"شماره سند","Alliance":"t2_OrderNumber","Address":"[t2].[OrderNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Prefactor_VchNo","DisplayName":"ش پیش فاکتور","Alliance":"t1_Prefactor_VchNo","Address":"[t1].[Prefactor_VchNo]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"t2_VoucherTypeStatus","DisplayName":"نوع سند","Alliance":"t2_VoucherTypeStatus","Address":"[t2].[VoucherTypeStatus]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.SaleOrderTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.Order","ColumnName":"tBase_Title","DisplayName":"مبنای حواله","Alliance":"tBase_Title","Address":"[tBase].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Seq","DisplayName":"ردیف","Alliance":"t1_Seq","Address":"[t1].[Seq]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t3_Type","DisplayName":"نوع","Alliance":"t3_Type","Address":"[t3].[Type]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Qty","DisplayName":"مقدار","Alliance":"t1_Qty","Address":"[t1].[Qty]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.OrderDetail","ColumnName":"t1_Description","DisplayName":"توضیحات","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""}]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"id_sod_serials","title":"سریال اقلام حواله فروش","dataActionName":"orderDetailSerials","colorClass":"btn-color-primary","iconClass":"ki-barcode ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var orderDetailId = ctx?.primaryKeyValue\n        || ctx?.primaryKeyValues?.[0]\n        || ctx?.selectedRow?.t1_Id\n        || ctx?.selectedRow?.id\n        || ctx?.selectedRow?.Id;\n\n    if (!orderDetailId) {\n        toastr.error(''لطفاً یک ردیف حواله فروش را انتخاب کنید'');\n        return;\n    }\n\n    appController.addPage(''/Panel/Sale/OrderDetail/ListByParentId?orderDetailId='' + orderDetailId, true, ''سریال‌های اقلام حواله فروش'');\n}"}]';

    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    -----------------------------------------------------------------------------
    PRINT N'=== [2/4] Upsert SavedQuery DataProfiles ===';
    -----------------------------------------------------------------------------

    IF OBJECT_ID('tempdb..#OdProfiles') IS NOT NULL DROP TABLE #OdProfiles;
    CREATE TABLE #OdProfiles
    (
        Name         NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title        NVARCHAR(200) NOT NULL,
        ColumnsJson  NVARCHAR(MAX) NOT NULL,
        CustomQuery  NVARCHAR(MAX) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #OdProfiles (Name, Title, ColumnsJson, CustomQuery) VALUES
        (N'OrderDetail_WithPrice', N'اقلام حواله فروش (با قیمت)', @ColumnsWithPrice, @BaseSelectWithPrice),
        (N'OrderDetail_NoPrice',   N'اقلام حواله فروش (بدون قیمت)', @ColumnsNoPrice, @BaseSelectNoPrice);

    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200), @PColumns NVARCHAR(MAX), @PSelect NVARCHAR(MAX), @PQueryJson NVARCHAR(MAX), @PId BIGINT;

    DECLARE od_profile_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, ColumnsJson, CustomQuery FROM #OdProfiles;
    OPEN od_profile_cur;
    FETCH NEXT FROM od_profile_cur INTO @PName, @PTitle, @PColumns, @PSelect;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @PColumns,
                EntityFullName = @EntityFullName,
                Mode = 1,
                Type = 1,
                ActionOptions = @ActionOptions,
                CustomActionButtonsJson = @CustomButtons,
                DiagramJson = N'{}',
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @PName;

            SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
            PRINT N'  UPDATED SavedQuery Name=' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
            (
                Name, Title, QueryJson, ColumnsJson, DiagramJson,
                CustomActionButtonsJson, EventScriptsJson,
                Mode, Type, EntityFullName, ActionOptions,
                CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
                IsActive
            )
            VALUES
            (
                @PName, @PTitle, @PQueryJson, @PColumns, N'{}',
                @CustomButtons, NULL,
                1, 1, @EntityFullName, @ActionOptions,
                1, 1, @SeedUser, @SeedUser,
                @Now, @NowShamsi, @Now, @NowShamsi,
                1
            );
            SET @PId = SCOPE_IDENTITY();
            PRINT N'  CREATED SavedQuery Name=' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END

        UPDATE #OdProfiles SET SavedQueryId = @PId WHERE Name = @PName;
        FETCH NEXT FROM od_profile_cur INTO @PName, @PTitle, @PColumns, @PSelect;
    END
    CLOSE od_profile_cur;
    DEALLOCATE od_profile_cur;

    -----------------------------------------------------------------------------
    PRINT N'=== [3/4] Rebuild RoleAccess DataProfile mappings ===';
    -----------------------------------------------------------------------------

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN #OdProfiles p ON p.SavedQueryId = ra.RowId
    WHERE ra.ActionAccessType = 3;
    PRINT N'  Cleared prior DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    IF OBJECT_ID('tempdb..#OdProfileRole') IS NOT NULL DROP TABLE #OdProfileRole;
    CREATE TABLE #OdProfileRole (ProfileName nvarchar(100) NOT NULL, RoleName nvarchar(200) NOT NULL);
    INSERT INTO #OdProfileRole (ProfileName, RoleName) VALUES
        (N'OrderDetail_WithPrice', N'Sale.OrderDetail.Manage'),
        (N'OrderDetail_WithPrice', N'Sale.OrderDetail.ViewWithPrice'),
        (N'OrderDetail_WithPrice', N'Sale.AfterSales'),
        (N'OrderDetail_WithPrice', N'ShowAllMenus'),
        (N'OrderDetail_NoPrice',   N'Sale.OrderDetail.View'),
        (N'OrderDetail_NoPrice',   N'Sale.AfterSales'),
        (N'OrderDetail_NoPrice',   N'ShowAllMenus');

    INSERT INTO system.RoleAccess
    (
        Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
        EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        N'dataProfile_' + CAST(p.SavedQueryId AS nvarchar(20)),
        3,
        NULL,
        @EntityFullNamePascal,
        p.Title,
        NULL,
        p.SavedQueryId,
        r.Id,
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    FROM #OdProfileRole map
    INNER JOIN #OdProfiles p ON p.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName;
    PRINT N'  Inserted DataProfile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4/4] Ensure Sell menu item for حواله های فروش ===';
    -----------------------------------------------------------------------------

    DECLARE @MenuId BIGINT = NULL;
    SELECT TOP 1 @MenuId = Id FROM system.SystemMenu WHERE Name = N'Sell' OR Title = N'فروش';

    IF @MenuId IS NULL
    BEGIN
        PRINT N'  WARN: Sell menu not found — skip menu update';
    END
    ELSE
    BEGIN
        DECLARE @Content NVARCHAR(MAX), @AccessRoles NVARCHAR(MAX), @AccessRoleIds NVARCHAR(MAX);
        SELECT @Content = Content, @AccessRoles = AccessRoles, @AccessRoleIds = AccessRoleIds
        FROM system.SystemMenu WHERE Id = @MenuId;

        -- Add menu item if missing
        IF CHARINDEX(N'/panel/sale/orderdetail/list', ISNULL(@Content, N'')) = 0
        BEGIN
            DECLARE @NewItem NVARCHAR(MAX) = N'{"text":"حواله های فروش","icon":"ki-delivery ki-outline","iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list","a_attr":{"href":"/panel/sale/orderdetail/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/sale/orderdetail/list"},"children":[]}';

            IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' OR @Content = N'[]'
                SET @Content = N'[' + @NewItem + N']';
            ELSE
                SET @Content = STUFF(@Content, LEN(@Content), 1, N',' + @NewItem + N']');

            PRINT N'  Added menu item حواله های فروش';
        END
        ELSE
            PRINT N'  Menu item حواله های فروش already present';

        -- Ensure AccessRoles includes OrderDetail roles (+ keep existing)
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName nvarchar(200) NOT NULL PRIMARY KEY, RoleId bigint NULL);

        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT j.value
        FROM OPENJSON(ISNULL(NULLIF(LTRIM(RTRIM(@AccessRoles)), N''), N'[]')) j
        WHERE j.value IS NOT NULL AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName
        FROM (VALUES
            (N'Sell.Menu'),
            (N'Sale.OrderDetail.Manage'),
            (N'Sale.OrderDetail.ViewWithPrice'),
            (N'Sale.OrderDetail.View'),
            (N'Sale.OrderDetail.ExportToExcel'),
            (N'ShowAllMenus')
        ) AS v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles m WHERE m.RoleName = v.RoleName);

        UPDATE m SET RoleId = r.Id
        FROM #MenuRoles m
        INNER JOIN system.Role r ON r.Name = m.RoleName;

        SELECT @AccessRoles =
            N'[' + STUFF((
                SELECT N',"'+ RoleName + N'"'
                FROM #MenuRoles
                ORDER BY RoleName
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        SELECT @AccessRoleIds =
            N'[' + STUFF((
                SELECT N',' + CAST(RoleId AS nvarchar(20))
                FROM #MenuRoles
                WHERE RoleId IS NOT NULL
                ORDER BY RoleId
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET Content = @Content,
            AccessRoles = @AccessRoles,
            AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;

        PRINT N'  Updated SystemMenu Id=' + CAST(@MenuId AS nvarchar(20)) + N' AccessRoles/Content';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_SaleOrderDetail_DataProfiles committed ===';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_SaleOrderDetail_DataProfiles rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- Verification
SELECT Id, Name, Title, EntityFullName, Mode, Type, IsActive
FROM system.SavedQuery
WHERE Name IN (N'OrderDetail_WithPrice', N'OrderDetail_NoPrice')
ORDER BY Name;

SELECT ra.Path, ra.ActionAccessType, ra.RowId, ra.DisplayName, r.Name AS RoleName
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND ra.EntityName LIKE N'%OrderDetail%'
ORDER BY ra.RowId, r.Name;

SELECT Id, Name, Title, AccessRoles, AccessRoleIds,
       CASE WHEN Content LIKE N'%/panel/sale/orderdetail/list%' THEN 1 ELSE 0 END AS HasOrderDetailMenu
FROM system.SystemMenu
WHERE Name = N'Sell' OR Title = N'فروش';

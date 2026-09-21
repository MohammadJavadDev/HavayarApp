/*
Patch_ProjectUtilizedMaterial_MissingMasters_2026-09-21.sql
ایجاد کالا/تفصیل جاافتاده برای ۳۵ ردیف کات‌اور اقلام مصرفی پروژه، سپس درج همان ردیف‌ها.

کالا از HTS Inv_Part (HtsId=Part_ID). واحد از Inv_PartUnit 21 → Hamkaran 2 → Inv.PartUnit Id=2.
نوع از راهکاران LGS3.Part.PartType=1 (همان الگوی کالاهای خواهر 987680/987681). HTS PartType_Fk=11 در جدول نوع هاویار نیست.
HamkaranId همان Hamkaran_Part_FK است (تکرار مجاز؛ یونیک نیست).

تفصیل: Acc_DL 16155 / Hamkaran_Acc_DL_FK=12410 / عنوان شيمي ساختمان.
در ERPS.FIN3.DL کدی با 12410 نیست؛ HamkaranId خالی می‌ماند تا با FIN.DL Id=12406 (HamkaranId=12410، محصولات غذایی بیژن) تداخل نکند.
نوع: Acc_DLType 4 شرکتها / RahkaranMatchId=2 → FIN.DLType Id=2.

Encoding: UTF-8 with BOM
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Patch_ProjectUtilizedMaterial_MissingMasters_2026-09-21.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), '/');
DECLARE @Seed NVARCHAR(80) = N'patch-pum-missing-masters-20260921';

BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM Inv.Part WHERE HtsId = 987706)
BEGIN
    INSERT INTO Inv.Part (
        Name, Code, LatinTitle, Number, Type, UnitId,
        [Foreign], EngineeringRoutine, DesignTypeIsRoutine, DocumentsNotRequired,
        PreciseTool, DataSheetUsage, Description, SaleRate, HamkaranId, HtsId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive)
    VALUES (
        N'میز', N'8004040004', N'', N'', 1, 2,
        0, 0, 0, 0,
        0, 0, N'', 1, 5002514, 987706,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi,
        1);
END

IF NOT EXISTS (SELECT 1 FROM Inv.Part WHERE HtsId = 987708)
BEGIN
    INSERT INTO Inv.Part (
        Name, Code, LatinTitle, Number, Type, UnitId,
        [Foreign], EngineeringRoutine, DesignTypeIsRoutine, DocumentsNotRequired,
        PreciseTool, DataSheetUsage, Description, SaleRate, HamkaranId, HtsId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive)
    VALUES (
        N'کمد (دارایی ثابت)', N'8004040006', N'', N'', 1, 2,
        0, 0, 0, 0,
        0, 0, N'', 1, 5002516, 987708,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi,
        1);
END

IF NOT EXISTS (SELECT 1 FROM FIN.DL WHERE TRY_CONVERT(INT, LTRIM(RTRIM(Code))) = 12410)
BEGIN
    INSERT INTO FIN.DL (
        Title, Code, TitleEnglish, TypeId, HamkaranId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        IsActive)
    VALUES (
        N'شيمي ساختمان', N'12410', NULL, 2, NULL,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi,
        1);
END

COMMIT;

SELECT p.Id, p.HtsId, p.HamkaranId, p.Code, p.Name
FROM Inv.Part p
WHERE p.HtsId IN (987706, 987708);

SELECT d.Id, d.Code, d.Title, d.TypeId, d.HamkaranId
FROM FIN.DL d
WHERE TRY_CONVERT(INT, LTRIM(RTRIM(d.Code))) = 12410;

IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;
SELECT ad.Acc_DL_ID AS HtsDlId, dl.Id AS DlId, CAST(ISNULL(dl.TypeId, 0) AS TINYINT) AS DlTypeId
INTO dbo._CutoverPumDlMap
FROM [TMS].[TotalSystem].[dbo].[Acc_DL] ad
INNER JOIN FIN.DL dl ON TRY_CONVERT(INT, LTRIM(RTRIM(dl.Code))) = ad.Hamkaran_Acc_DL_FK;
CREATE UNIQUE CLUSTERED INDEX IX_CutoverPumDlMap ON dbo._CutoverPumDlMap (HtsDlId);

IF OBJECT_ID('tempdb..#PumSrc') IS NOT NULL DROP TABLE #PumSrc;
SELECT
    CAST(s.Id AS BIGINT) AS HtsId,
    s.VchItemId, s.VchHdrId, s.DlId AS HtsDlId, s.[Year], s.VchNum,
    s.VchDate, s.VchDateInText, s.VchTypeId, s.Qty, s.PartId AS HtsPartId,
    s.ReplacedPartId AS HtsReplacedPartId, s.ReplacedQty, s.Comment
INTO #PumSrc
FROM [TMS].[TotalSystem].[dbo].[Sale_ProjectUtilizedMaterial] s
WHERE s.PartId IN (987706, 987708) OR s.DlId = 16155;

IF OBJECT_ID('tempdb..#PumMapped') IS NOT NULL DROP TABLE #PumMapped;
SELECT
    s.*,
    p.Id AS PartId,
    d.DlId,
    d.DlTypeId,
    rp.Id AS ReplacedPartId
INTO #PumMapped
FROM #PumSrc s
INNER JOIN Inv.Part p ON p.HtsId = s.HtsPartId
INNER JOIN dbo._CutoverPumDlMap d ON d.HtsDlId = s.HtsDlId
LEFT JOIN Inv.Part rp ON rp.HtsId = s.HtsReplacedPartId AND ISNULL(s.HtsReplacedPartId, 0) <> 0;

DECLARE @Skipped INT = (SELECT COUNT(*) FROM #PumSrc s WHERE NOT EXISTS (SELECT 1 FROM #PumMapped m WHERE m.HtsId = s.HtsId));

INSERT INTO Sale.ProjectUtilizedMaterial (
    DLId, [Year], VchNum, VchMiladiDate, VchDateShamsiDate, Qty, PartId,
    DLTypeId, Comment, HtsId, ReplacedPartId, ReplacedQty, VchHdrId, VchItemId, VchTypeId,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
    IsActive)
SELECT
    m.DlId, m.[Year], m.VchNum, m.VchDate, m.VchDateInText, m.Qty, m.PartId,
    m.DlTypeId, m.Comment, m.HtsId, m.ReplacedPartId, ISNULL(m.ReplacedQty, 0),
    m.VchHdrId, m.VchItemId, m.VchTypeId,
    1, @Seed, @Now, @NowShamsi,
    1, @Seed, @Now, @NowShamsi,
    1
FROM #PumMapped m
WHERE NOT EXISTS (SELECT 1 FROM Sale.ProjectUtilizedMaterial t WHERE t.HtsId = m.HtsId);

DECLARE @Inserted INT = @@ROWCOUNT;

IF OBJECT_ID(N'dbo._CutoverPumDlMap') IS NOT NULL DROP TABLE dbo._CutoverPumDlMap;

PRINT N'=== DONE Patch_ProjectUtilizedMaterial_MissingMasters ===';
PRINT N'Inserted materials=' + CAST(@Inserted AS NVARCHAR(20)) + N' skipped=' + CAST(@Skipped AS NVARCHAR(20));

SELECT
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterial) AS MaterialCnt,
    (SELECT COUNT(*) FROM Sale.ProjectUtilizedMaterialChange) AS ChangeCnt,
    @Inserted AS InsertedCnt,
    @Skipped AS SkippedCnt;

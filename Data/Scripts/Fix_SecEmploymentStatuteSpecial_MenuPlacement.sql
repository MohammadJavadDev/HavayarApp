/*
Fix_SecEmploymentStatuteSpecial_MenuPlacement.sql
برگ را زیر «منابع انسانی / دبیرخانه» موجود می‌گذارد و ریشه تکراری را برمی‌دارد.
UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-employment-statute-special-menu';
DECLARE @Path NVARCHAR(200) = N'/panel/sec/employmentstatutespecial/list';
DECLARE @LeafOnly NVARCHAR(MAX) = N'{"text":"لیست استثنائات سابقه حضور","icon":"ki-calendar ki-outline","iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list","a_attr":{"href":"/panel/sec/employmentstatutespecial/list"},"data":{"iconColor":"#a78bfa","path":"/panel/sec/employmentstatutespecial/list"},"children":[]}';

DECLARE @MenuId BIGINT;
DECLARE @Content NVARCHAR(MAX);
SELECT TOP 1 @MenuId = Id, @Content = Content
FROM system.SystemMenu
WHERE Name = N'AllMenus'
ORDER BY Id;

IF @MenuId IS NULL OR ISJSON(@Content) = 0
    THROW 51210, N'منوی AllMenus پیدا نشد یا JSON نامعتبر است', 1;

DECLARE @HrIdx INT = NULL;
SELECT TOP 1 @HrIdx = CAST(hr.[key] AS INT)
FROM OPENJSON(@Content) hr
WHERE JSON_VALUE(hr.value, '$.text') IN (N'منابع انسانی', N'سیستم منابع انسانی')
  AND EXISTS (
      SELECT 1
      FROM OPENJSON(hr.value, '$.children') ch
      WHERE JSON_VALUE(ch.value, '$.text') = N'دبیرخانه'
        AND EXISTS (
            SELECT 1
            FROM OPENJSON(ch.value, '$.children') leaf
            WHERE JSON_VALUE(leaf.value, '$.path') = N'/panel/sec/personnel/list'
        )
  )
ORDER BY CAST(hr.[key] AS INT);

IF @HrIdx IS NULL
    THROW 51211, N'زیرمنوی دبیرخانه پرسنل پیدا نشد', 1;

DECLARE @SecIdx INT = NULL;
SELECT TOP 1 @SecIdx = CAST([key] AS INT)
FROM OPENJSON(JSON_QUERY(@Content, '$[' + CAST(@HrIdx AS nvarchar(10)) + '].children'))
WHERE JSON_VALUE([value], '$.text') = N'دبیرخانه';

DECLARE @SecJson NVARCHAR(MAX) = JSON_QUERY(@Content, '$[' + CAST(@HrIdx AS nvarchar(10)) + '].children[' + CAST(@SecIdx AS nvarchar(10)) + ']');
IF CHARINDEX(N'"path":"' + @Path + N'"', @SecJson) = 0
BEGIN
    SET @Content = JSON_MODIFY(@Content,
        N'append $[' + CAST(@HrIdx AS nvarchar(10)) + N'].children[' + CAST(@SecIdx AS nvarchar(10)) + N'].children',
        JSON_QUERY(@LeafOnly));
    PRINT N'  برگ زیر دبیرخانه موجود اضافه شد';
END
ELSE
    PRINT N'  برگ از قبل زیر دبیرخانه موجود است';

DECLARE @DupIdx INT = NULL;
SELECT TOP 1 @DupIdx = CAST(hr.[key] AS INT)
FROM OPENJSON(@Content) hr
WHERE JSON_VALUE(hr.value, '$.text') = N'سیستم منابع انسانی'
  AND EXISTS (
      SELECT 1 FROM OPENJSON(hr.value, '$.children') ch
      WHERE JSON_VALUE(ch.value, '$.text') = N'دبیرخانه'
        AND NOT EXISTS (
            SELECT 1 FROM OPENJSON(ch.value, '$.children') leaf
            WHERE JSON_VALUE(leaf.value, '$.path') = N'/panel/sec/personnel/list'
        )
        AND EXISTS (
            SELECT 1 FROM OPENJSON(ch.value, '$.children') leaf
            WHERE JSON_VALUE(leaf.value, '$.path') = @Path
        )
  )
ORDER BY CAST(hr.[key] AS INT) DESC;

IF @DupIdx IS NOT NULL
BEGIN
    SELECT @Content = N'[' + STRING_AGG(CAST(j.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS INT)) + N']'
    FROM OPENJSON(@Content) j
    WHERE j.type <> 0 AND CAST(j.[key] AS INT) <> @DupIdx;
    PRINT N'  ریشه تکراری حذف شد';
END

IF ISJSON(@Content) = 0
    THROW 51212, N'Content تولیدشده JSON معتبر نیست', 1;

UPDATE system.SystemMenu
SET Content = @Content,
    ModifiedById = 1,
    ModifiedByName = @SeedUser,
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Id = @MenuId;

PRINT N'=== DONE Fix menu placement ===';

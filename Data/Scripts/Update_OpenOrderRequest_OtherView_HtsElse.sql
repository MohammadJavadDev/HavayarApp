/*
  مشاهده عمومی = شاخه else صفحه 74 قدیم:
  IsDeleted = 0 AND EngineeringAccept = 1 AND RequestedPersonel IS NOT NULL
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Name nvarchar(100) = N'vw_OpenOrderRequestOtherView';
DECLARE @Id bigint;
DECLARE @Qj nvarchar(max);

SELECT @Id = q.Id, @Qj = q.QueryJson
FROM system.SavedQuery AS q
WHERE q.Name = @Name;

IF @Id IS NULL
    THROW 51031, N'profile vw_OpenOrderRequestOtherView was not found.', 1;

DECLARE @Old nvarchar(max) = N'\r\n    AND (\r\n        EXISTS (\r\n            SELECT 1\r\n            FROM OPENJSON(CASE WHEN ISJSON([t1].[RequestedPersonelIds]) = 1 THEN [t1].[RequestedPersonelIds] ELSE N''[]'' END) AS [rp]\r\n            WHERE TRY_CAST([rp].[value] AS bigint) = TRY_CAST(@CurrentUserId AS bigint)\r\n        )\r\n        OR EXISTS (\r\n            SELECT 1\r\n            FROM OPENJSON(CASE WHEN ISJSON([t1].[RequestedEngineeringPersonelIds]) = 1 THEN [t1].[RequestedEngineeringPersonelIds] ELSE N''[]'' END) AS [re]\r\n            WHERE TRY_CAST([re].[value] AS bigint) = TRY_CAST(@CurrentUserId AS bigint)\r\n        )\r\n    )';

DECLARE @New nvarchar(max) = N'\r\n    AND [t1].[RequestedPersonel] IS NOT NULL';

IF CHARINDEX(@Old, @Qj) = 0
    THROW 51032, N'OtherView user-match filter text was not found.', 1;

IF (LEN(@Qj) - LEN(REPLACE(@Qj, @Old, N''))) / LEN(@Old) <> 1
    THROW 51033, N'OtherView user-match filter text appears more than once.', 1;

DECLARE @NewQj nvarchar(max) = REPLACE(@Qj, @Old, @New);

IF CHARINDEX(N'[t1].[IsDeleted] = 0', @NewQj) = 0
    OR CHARINDEX(N'[t1].[EngineeringAccept] = 1', @NewQj) = 0
    OR CHARINDEX(N'[t1].[RequestedPersonel] IS NOT NULL', @NewQj) = 0
    OR CHARINDEX(N'TRY_CAST(@CurrentUserId', @NewQj) > 0
    OR CHARINDEX(N'RequestedPersonelIds', @NewQj) > 0
    THROW 51034, N'OtherView filter verification failed before save.', 1;

UPDATE system.SavedQuery
SET QueryJson = @NewQj
WHERE Id = @Id;

DECLARE @Check nvarchar(max) = (SELECT QueryJson FROM system.SavedQuery WHERE Id = @Id);

IF CHARINDEX(N'[t1].[RequestedPersonel] IS NOT NULL', @Check) = 0
    OR CHARINDEX(N'RequestedPersonelIds', @Check) > 0
    OR CHARINDEX(N'TRY_CAST(@CurrentUserId', @Check) > 0
    THROW 51035, N'OtherView filter verification failed after save.', 1;

PRINT N'OtherView filter updated.';

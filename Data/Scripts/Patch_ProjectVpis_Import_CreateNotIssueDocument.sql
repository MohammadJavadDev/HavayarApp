/*
  بعد از درج VPIS جدید در ورود اکسل، همان مدرک اولیه صفحه تکی ساخته می‌شود:
  ریویژن 0، وضعیت NotIssue (1)، آخرین ریویژن.
  فقط اگر هنوز این INSERT در کوئری نباشد.
*/
SET NOCOUNT ON;

DECLARE @Old NVARCHAR(MAX) = N'@CurrentDateTimeMiladi, @CurrentDateTimeShamsi, @IsActive, @ReviewerNames, @ReviewersId);';
DECLARE @New NVARCHAR(MAX) = N'@CurrentDateTimeMiladi, @CurrentDateTimeShamsi, @IsActive, @ReviewerNames, @ReviewersId);

DECLARE @NewProjectVpisId bigint = SCOPE_IDENTITY();

INSERT INTO Edms.Document
    (ProjectId, DocumentVpisId, Status, Revision, IsLatest, IsActive,
     ApproverId, ReviewerId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
VALUES
    (@ProjectNameId, @NewProjectVpisId, 1, 0, 1, 1,
     @ApproverId, @ReviewersId,
     @CurrentUserId, @CurrentUserId, @CurrentUserName, @CurrentUserName,
     @CurrentDateTimeMiladi, @CurrentDateTimeShamsi,
     @CurrentDateTimeMiladi, @CurrentDateTimeShamsi);
';

UPDATE system.ImportDefinition
SET SqlQuery = REPLACE(SqlQuery, @Old, @New),
    ModifiedDateMiladiDateTime = GETDATE()
WHERE Id = 7
  AND SqlQuery NOT LIKE N'%INSERT INTO Edms.Document%';

SELECT Id, Title,
       CASE WHEN SqlQuery LIKE N'%INSERT INTO Edms.Document%' THEN 1 ELSE 0 END AS CreatesNotIssueDocument
FROM system.ImportDefinition
WHERE Id = 7;

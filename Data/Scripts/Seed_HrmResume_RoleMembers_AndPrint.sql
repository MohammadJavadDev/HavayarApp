/*
Seed_HrmResume_RoleMembers_AndPrint.sql
عضویت گروه‌های ۴۶۷ تا ۴۷۱ و قالب چاپ پرسشنامه استخدامی.
UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-resume-access';

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد. عضویت نقش‌ها اعمال نشد.', 16, 1);
    RETURN;
END;

DECLARE @Map TABLE (GroupId INT, RoleName NVARCHAR(200));
INSERT INTO @Map VALUES
 (467, N'رزومه - کارشناس توسعه منابع انسانی'),
 (468, N'رزومه - مدیر/سرپرست واحد درخواست کننده نیرو'),
 (469, N'رزومه - مدیر/سرپرست توسعه منابع انسانی'),
 (470, N'رزومه - هیئت مدیره'),
 (471, N'رزومه - کارشناس امور اداری تهران/کارخانه');

IF OBJECT_ID('tempdb..#Resolved') IS NOT NULL DROP TABLE #Resolved;
SELECT DISTINCT su.Id AS UserId, r.Id AS RoleId, r.Name AS RoleName
INTO #Resolved
FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
INNER JOIN @Map map ON map.GroupId = m.UserGroup_FK
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = m.User_FK AND hu.IsActive = 1
INNER JOIN system.[User] su ON su.IsActive = 1 AND (
    LOWER(su.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(hu.ActiveDirectoryUsername)), N''), hu.Username))
    OR LOWER(su.Username) = LOWER(hu.Username))
INNER JOIN system.Role r ON r.Name = map.RoleName;

DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200), @Assigned INT = 0;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT UserId, RoleId, RoleName FROM #Resolved;
OPEN c;
FETCH NEXT FROM c INTO @UserId, @RoleId, @RoleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE u.Id = @UserId AND TRY_CAST(j.value AS BIGINT) = @RoleId)
    BEGIN
        UPDATE system.[User]
        SET RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]') THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']')
            END,
            Roles = CASE
                WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') THEN N'["' + STRING_ESCAPE(@RoleName, 'json') + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + STRING_ESCAPE(@RoleName, 'json') + N'"]')
            END,
            ModifiedById = 1, ModifiedByName = @Seed, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @UserId;
        SET @Assigned = @Assigned + 1;
    END
    FETCH NEXT FROM c INTO @UserId, @RoleId, @RoleName;
END
CLOSE c;
DEALLOCATE c;

DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM system.SystemMenu WHERE Name = N'AllMenus');
DECLARE @AllNames NVARCHAR(MAX) = (SELECT AccessRoles FROM system.SystemMenu WHERE Name = N'AllMenus');
SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
FROM (
    SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
    UNION
    SELECT r.Id FROM system.Role r INNER JOIN @Map m ON m.RoleName = r.Name
) x WHERE x.Id IS NOT NULL;
SELECT @AllNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
FROM (
    SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllNames, N'[]')) j
    UNION
    SELECT r.Name FROM system.Role r INNER JOIN @Map m ON m.RoleName = r.Name
) x;
UPDATE system.SystemMenu SET AccessRoleIds = @AllIds, AccessRoles = @AllNames WHERE Name = N'AllMenus';

DECLARE @Base NVARCHAR(MAX) = N'
SELECT v.Section, v.Title, v.Detail
FROM Hrm.Resume r
CROSS APPLY (VALUES
 (N''مشخصات فردی'', N''کد'', CAST(r.HtsId AS nvarchar(40))),
 (N''مشخصات فردی'', N''تاریخ ثبت'', r.ResumeShamsiDate),
 (N''مشخصات فردی'', N''نام'', r.Name),
 (N''مشخصات فردی'', N''نام خانوادگی'', r.Family),
 (N''مشخصات فردی'', N''نام پدر'', r.FatherName),
 (N''مشخصات فردی'', N''تاریخ تولد'', r.BirthDate),
 (N''مشخصات فردی'', N''محل تولد'', r.BirthPlace),
 (N''مشخصات فردی'', N''محل صدور'', r.IssuePlace),
 (N''مشخصات فردی'', N''دین'', r.Religion),
 (N''مشخصات فردی'', N''تعداد فرزند'', CAST(r.NumberOfChildren AS nvarchar(20))),
 (N''مشخصات فردی'', N''علت معافیت'', r.MilitaryExemptionReason),
 (N''مشخصات فردی'', N''جنسیت'', CASE r.Sex WHEN 2 THEN N''مرد'' ELSE N''زن'' END),
 (N''اطلاعات تماس'', N''تلفن'', r.PhoneNumber),
 (N''اطلاعات تماس'', N''موبایل'', r.MobileNumber),
 (N''اطلاعات تماس'', N''ایمیل'', r.EmailAddress),
 (N''اطلاعات تماس'', N''آدرس'', r.HomeAddress),
 (N''تحصیلات'', N''مقطع'', CASE r.Education WHEN 5 THEN N''دیپلم'' WHEN 6 THEN N''فوق دیپلم'' WHEN 7 THEN N''لیسانس'' WHEN 8 THEN N''فوق لیسانس'' WHEN 9 THEN N''دکتری'' ELSE CAST(r.Education AS nvarchar(10)) END)
) v(Section, Title, Detail)
WHERE r.Id = TRY_CAST(@resumeId AS bigint)
UNION ALL
SELECT N''زبان'', ISNULL(l.Title, N''''), CAST(x.KnowledgeLevel AS nvarchar(20))
FROM Hrm.ResumeLanguage x LEFT JOIN Hrm.TrainingLanguage l ON l.Id = x.LanguageId
WHERE x.ResumeId = TRY_CAST(@resumeId AS bigint)
UNION ALL
SELECT N''نرم‌افزار'', COALESCE(s.Title, x.OtherSoftware), CAST(x.KnowledgeLevel AS nvarchar(20))
FROM Hrm.ResumeSoftware x LEFT JOIN Hrm.TrainingSoftware s ON s.Id = x.SoftwareId
WHERE x.ResumeId = TRY_CAST(@resumeId AS bigint)
UNION ALL
SELECT N''مهارت'', sk.Title, N''''
FROM Hrm.ResumeSkill x LEFT JOIN Hrm.Skill sk ON sk.Id = x.SkillId
WHERE x.ResumeId = TRY_CAST(@resumeId AS bigint)
UNION ALL
SELECT N''سابقه کار'', x.CompanyName, x.OrgPost
FROM Hrm.ResumeWorkExperience x
WHERE x.ResumeId = TRY_CAST(@resumeId AS bigint)
UNION ALL
SELECT N''اطلاعات تکمیلی فرم کارجو'', f.QuestionTitle, ISNULL(f.Answer, N'''') + N'' '' + ISNULL(f.Explain, N'''')
FROM Hrm.ResumeFurtherInformation f
WHERE f.ResumeId = TRY_CAST(@resumeId AS bigint)';

DECLARE @Mrt NVARCHAR(MAX) = N'{"ReportVersion":"2024.3.2.0","ReportGuid":"a7c3e1b2-4d50-4f11-9a22-resumeprint01","ReportName":"HrmResumePrint","ReportAlias":"پرسشنامه استخدامی","EngineVersion":"EngineV2","CalculationMode":"Interpretation","ReportUnit":"Centimeters","Dictionary":{"DataSources":{"0":{"Ident":"StiDataTableSource","Name":"dtRows","Alias":"dtRows","Key":"dtRows","Columns":{"0":{"Name":"Section","Index":-1,"NameInSource":"Section","Alias":"Section","Type":"System.String"},"1":{"Name":"Title","Index":-1,"NameInSource":"Title","Alias":"Title","Type":"System.String"},"2":{"Name":"Detail","Index":-1,"NameInSource":"Detail","Alias":"Detail","Type":"System.String"}},"NameInSource":"NewDataSet.dtRows"}}},"Pages":{"0":{"Ident":"StiPage","Name":"Page1","Guid":"a7c3e1b2-4d50-4f11-9a22-resumeprint01","Border":";;2;;;;;solid:Black","Brush":"solid:","Components":{"0":{"Ident":"StiReportTitleBand","Name":"Title","ClientRectangle":"0,0,19.0,1.2","Components":{"0":{"Ident":"StiText","Name":"title","ClientRectangle":"0,0,19,1.2","Text":{"Value":"پرسشنامه استخدامی گروه صنعتی هوایار"},"HorAlignment":"Center","VertAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;18;;","Border":";;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true},"Margins":{"Left":2,"Right":2,"Top":0,"Bottom":0}}},"Brush":"solid:"},"1":{"Ident":"StiHeaderBand","Name":"Head","ClientRectangle":"0,1.4,19,0.7","Components":{"0":{"Ident":"StiText","Name":"h1","ClientRectangle":"0,0,4,0.7","Text":{"Value":"بخش"},"HorAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true}},"1":{"Ident":"StiText","Name":"h2","ClientRectangle":"4,0,5,0.7","Text":{"Value":"عنوان"},"HorAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true}},"2":{"Ident":"StiText","Name":"h3","ClientRectangle":"9,0,10,0.7","Text":{"Value":"مقدار"},"HorAlignment":"Center","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true}}},"Brush":"solid:230,230,230"},"2":{"Ident":"StiDataBand","Name":"Data","ClientRectangle":"0,2.2,19,0.7","Components":{"0":{"Ident":"StiText","Name":"d1","ClientRectangle":"0,0,4,0.7","Text":{"Value":"{dtRows.Section}"},"HorAlignment":"Right","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true}},"1":{"Ident":"StiText","Name":"d2","ClientRectangle":"4,0,5,0.7","Text":{"Value":"{dtRows.Title}"},"HorAlignment":"Right","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true}},"2":{"Ident":"StiText","Name":"d3","ClientRectangle":"9,0,10,0.7","Text":{"Value":"{dtRows.Detail}"},"HorAlignment":"Right","Font":"IRANSansWeb(FaNum) Medium;9;;","Border":"All;;;;;;;solid:Black","Brush":"solid:","TextBrush":"solid:Black","TextOptions":{"RightToLeft":true,"WordWrap":true}}},"DataSourceName":"dtRows","PrintIfEmpty":true,"CanGrow":true,"Brush":"solid:"}},"PaperSize":"A4","PageWidth":21,"PageHeight":29.7,"RightToLeft":true,"Watermark":{"TextBrush":"solid:50,0,0,0"},"Margins":{"Left":1,"Right":1,"Top":1,"Bottom":1}}}}';

IF EXISTS (SELECT 1 FROM dbo.ReportBuilderReport WHERE Name = N'HrmResumePrint')
    UPDATE dbo.ReportBuilderReport
    SET Title = N'پرسشنامه استخدامی', ObjectType = N'Query', Type = 1, BaseQuery = @Base, Content = @Mrt, IsActive = 1,
        ModifiedById = 1, ModifiedByName = @Seed, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'HrmResumePrint';
ELSE
    INSERT INTO dbo.ReportBuilderReport
        (Name, Title, ObjectType, Type, BaseQuery, Content, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (N'HrmResumePrint', N'پرسشنامه استخدامی', N'Query', 1, @Base, @Mrt, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1);

DECLARE @ReportId BIGINT = (SELECT Id FROM dbo.ReportBuilderReport WHERE Name = N'HrmResumePrint');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportBuilderReportSelect WHERE ReportBuilderReportId = @ReportId AND Title = N'dtRows')
    INSERT INTO dbo.ReportBuilderReportSelect
        (Title, ReportBuilderReportId, CreatedById, CreatedByName, ModifiedById, ModifiedByName, CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES (N'dtRows', @ReportId, 1, @Seed, 1, @Seed, @Now, @Now, @NowShamsi, @NowShamsi, 1);

SELECT @Assigned AS AssignedMembers;
SELECT Name, Title FROM dbo.ReportBuilderReport WHERE Name = N'HrmResumePrint';
SELECT RoleName, COUNT(*) AS Members FROM #Resolved GROUP BY RoleName;

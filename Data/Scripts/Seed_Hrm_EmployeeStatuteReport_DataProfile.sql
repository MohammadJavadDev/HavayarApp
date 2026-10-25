/*
  گزارش سابقه حضور پرسنل — معادل HTS صفحه EmployeeStatute (346) / hrm-employeestatute.
  نام یکتا: Hrm_EmployeeStatuteReport
  یونیکد صفحه: HrmEmployeeStatuteReport
  صفحه: /panel/hrm/employeestatutereport/list

  محاسبه سال/ماه حضور همان GetFinalData در EmployeeStatuteController:
  حکم استخدام (۱ و ۱۲۱۰۰۰۰۰۴) و پایان خدمت (۳ و ۱۲۱۰۰۰۰۰۶)،
  تاریخ استخدام واقعی و ماه/سال افزایشی از Sec.EmploymentStatuteSpecial،
  سپس باشگاه هوایاری.

  UTF-8 with BOM. Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Hrm_EmployeeStatuteReport_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-hrm-employee-statute-report';

    DECLARE @Name NVARCHAR(200) = N'Hrm_EmployeeStatuteReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش سابقه حضور پرسنل';
    DECLARE @EntityFullName NVARCHAR(200) = N'HrmEmployeeStatuteReport';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/hrm/employeestatutereport/list';
    DECLARE @RoleView NVARCHAR(200) = N'Hrm.EmployeeStatuteReport.View';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'WITH RawStatute AS (
    SELECT
        e.EmployeeID,
        e.Code COLLATE DATABASE_DEFAULT AS PersCode,
        p.FirstName COLLATE DATABASE_DEFAULT AS FirstName,
        p.LastName COLLATE DATABASE_DEFAULT AS LastName,
        ls.EmployeeStatuteID,
        CAST(ls.ApplyDate AS date) AS ApplyDate,
        CASE
            WHEN st.StatuteTypeID IN (1, 121000004) THEN 1
            WHEN st.StatuteTypeID IN (3, 121000006) THEN 3
            ELSE NULL
        END AS IssueTimeCode
    FROM ERPS.Erps.HCM3.Employee AS e
    LEFT JOIN ERPS.Erps.GNR3.Party AS p ON p.PartyID = e.PartyRef
    INNER JOIN ERPS.Erps.HCM3.EmployeeStatute AS ls ON ls.EmployeeRef = e.EmployeeID
    INNER JOIN ERPS.Erps.HCM3.StatuteType AS st ON st.StatuteTypeID = ls.StatuteTypeRef
    WHERE (
            st.StatuteTypeID IN (1, 121000004, 3, 121000006)
            AND (
                e.Status = 2
                OR e.Code IN (N''84024'', N''84043'', N''87116'', N''97014'', N''07057'', N''93035'')
            )
        )
        OR e.Code = N''82043''
),
Events AS (
    SELECT * FROM RawStatute WHERE IssueTimeCode IN (1, 3)
),
Starts AS (
    SELECT
        EmployeeID,
        EmployeeStatuteID,
        ApplyDate,
        ROW_NUMBER() OVER (PARTITION BY EmployeeID ORDER BY ApplyDate, EmployeeStatuteID) AS StartRn
    FROM Events
    WHERE IssueTimeCode = 1
),
LastStart AS (
    SELECT EmployeeID, ApplyDate, StartRn
    FROM (
        SELECT
            EmployeeID,
            ApplyDate,
            StartRn,
            ROW_NUMBER() OVER (PARTITION BY EmployeeID ORDER BY ApplyDate DESC, EmployeeStatuteID DESC) AS RevRn
        FROM Starts
    ) s
    WHERE RevRn = 1
),
FirstStart AS (
    SELECT EmployeeID, ApplyDate FROM Starts WHERE StartRn = 1
),
LastEvent AS (
    SELECT EmployeeID, IssueTimeCode
    FROM (
        SELECT
            EmployeeID,
            IssueTimeCode,
            ROW_NUMBER() OVER (PARTITION BY EmployeeID ORDER BY ApplyDate DESC, EmployeeStatuteID DESC) AS RevRn
        FROM Events
    ) e
    WHERE RevRn = 1
),
PersonOne AS (
    SELECT
        Id,
        Code COLLATE DATABASE_DEFAULT AS Code,
        Name,
        Family,
        BranchCode,
        RealyBirthDate,
        WorkExperienceOutOfHavayarYear,
        WorkExperienceOutOfHavayarMonth,
        OrgUnitId,
        OrgPostId,
        ROW_NUMBER() OVER (PARTITION BY Code ORDER BY Id) AS Rn
    FROM Hcm.Personel
    WHERE Code IS NOT NULL
),
SpecialOne AS (
    SELECT
        PersonelId,
        CAST(RealEmploymentMiladiDate AS date) AS RealDate,
        ISNULL(AdditionalYear, 0) * 365 + ISNULL(AdditionalMonth, 0) * 30 AS ExtraDays,
        ROW_NUMBER() OVER (PARTITION BY PersonelId ORDER BY Id) AS Rn
    FROM Sec.EmploymentStatuteSpecial
),
Emp AS (
    SELECT DISTINCT EmployeeID, PersCode, FirstName, LastName
    FROM Events
),
Closed AS (
    SELECT
        ev.EmployeeID,
        SUM(DATEDIFF(DAY, ev.ApplyDate,
            CASE WHEN st.StartRn = 1 THEN COALESCE(sp.RealDate, st.ApplyDate) ELSE st.ApplyDate END
        )) AS ClosedDays
    FROM Events ev
    INNER JOIN Emp em ON em.EmployeeID = ev.EmployeeID
    LEFT JOIN PersonOne per ON per.Code = em.PersCode AND per.Rn = 1
    LEFT JOIN SpecialOne sp ON sp.PersonelId = per.Id AND sp.Rn = 1
    OUTER APPLY (
        SELECT TOP 1 s.ApplyDate, s.StartRn
        FROM Starts s
        WHERE s.EmployeeID = ev.EmployeeID
          AND (
                s.ApplyDate < ev.ApplyDate
                OR (s.ApplyDate = ev.ApplyDate AND s.EmployeeStatuteID < ev.EmployeeStatuteID)
          )
        ORDER BY s.ApplyDate DESC, s.EmployeeStatuteID DESC
    ) st
    WHERE ev.IssueTimeCode = 3
      AND st.ApplyDate IS NOT NULL
    GROUP BY ev.EmployeeID
),
Calc AS (
    SELECT
        em.EmployeeID,
        em.PersCode,
        COALESCE(em.FirstName, per.Name) AS FirstName,
        COALESCE(em.LastName, per.Family) AS LastName,
        ou.Title AS DepartmentTitle,
        op.Title AS PostTitle,
        CASE
            WHEN per.BranchCode = 1 THEN N''تهران''
            WHEN per.BranchCode = 2 THEN N''کرج''
            ELSE N''نامشخص''
        END AS WorkLocationDesc,
        COALESCE(sp.RealDate, fs.ApplyDate) AS EmploymentDate,
        CASE WHEN per.RealyBirthDate IS NULL THEN NULL ELSE CAST(per.RealyBirthDate AS date) END AS BirthDate,
        per.WorkExperienceOutOfHavayarYear AS WorkExperienceYear,
        per.WorkExperienceOutOfHavayarMonth AS WorkExperienceMonth,
        CASE
            WHEN rawDays < 0 THEN -rawDays
            ELSE rawDays
        END + ISNULL(sp.ExtraDays, 0) AS TotalDays
    FROM Emp em
    INNER JOIN LastStart ls ON ls.EmployeeID = em.EmployeeID
    INNER JOIN FirstStart fs ON fs.EmployeeID = em.EmployeeID
    INNER JOIN LastEvent le ON le.EmployeeID = em.EmployeeID
    LEFT JOIN Closed c ON c.EmployeeID = em.EmployeeID
    LEFT JOIN PersonOne per ON per.Code = em.PersCode AND per.Rn = 1
    LEFT JOIN SpecialOne sp ON sp.PersonelId = per.Id AND sp.Rn = 1
    LEFT JOIN Hrm.OrgUnit ou ON ou.Id = per.OrgUnitId
    LEFT JOIN Hrm.OrgPost op ON op.Id = per.OrgPostId
    CROSS APPLY (
        SELECT CASE
            WHEN le.IssueTimeCode = 1 AND ls.StartRn > 1 THEN
                ISNULL(c.ClosedDays, 0) + DATEDIFF(DAY, CAST(GETDATE() AS date),
                    CASE WHEN ls.StartRn = 1 THEN COALESCE(sp.RealDate, ls.ApplyDate) ELSE ls.ApplyDate END)
            ELSE
                DATEDIFF(DAY, CAST(GETDATE() AS date),
                    CASE WHEN ls.StartRn = 1 THEN COALESCE(sp.RealDate, ls.ApplyDate) ELSE ls.ApplyDate END)
        END AS rawDays
    ) days
)
--!--mainsection
SELECT
    CAST(c.EmployeeID AS bigint) AS [t1_Id],
    c.PersCode AS [t1_PersCode],
    c.FirstName AS [t1_FirstName],
    c.LastName AS [t1_LastName],
    c.DepartmentTitle AS [t1_DepartmentTitle],
    c.PostTitle AS [t1_PostTitle],
    c.WorkLocationDesc AS [t1_WorkLocationDesc],
    CASE
        WHEN c.TotalDays / 365 >= 25 THEN N''25 ساله‌ها''
        WHEN c.TotalDays / 365 >= 20 THEN N''20 ساله‌ها''
        WHEN c.TotalDays / 365 >= 16 THEN N''16 ساله‌ها''
        WHEN c.TotalDays / 365 >= 12 THEN N''12 ساله‌ها''
        WHEN c.TotalDays / 365 >= 8 THEN N''8 ساله‌ها''
        WHEN c.TotalDays / 365 >= 4 THEN N''4 ساله‌ها''
        ELSE N''''
    END AS [t1_HavayarClub],
    CASE WHEN c.EmploymentDate IS NULL THEN NULL ELSE dbo.ToShamsiDate(c.EmploymentDate, N''/'') END AS [t1_EmploymentShamsiDate],
    dbo.ToShamsiDate(DATEADD(DAY, -c.TotalDays, CAST(GETDATE() AS date)), N''/'') AS [t1_RealAnniversaryShamsiDate],
    c.TotalDays / 365 AS [t1_PresenceYears],
    (c.TotalDays % 365) / 30 AS [t1_PresenceMonth],
    c.WorkExperienceYear AS [t1_WorkExperienceYear],
    c.WorkExperienceMonth AS [t1_WorkExperienceMonth],
    CASE WHEN c.BirthDate IS NULL THEN NULL ELSE dbo.ToShamsiDate(c.BirthDate, N''/'') END AS [t1_BirthShamsiDate]
FROM Calc c';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"ERPS.Erps.HCM3.Employee","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"CAST(c.EmployeeID AS bigint)","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"ERPS.Erps.HCM3.Employee","ColumnName":"t1_PersCode","DisplayName":"کد پرسنلی","Alliance":"t1_PersCode","Address":"c.PersCode","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"ERPS.Erps.GNR3.Party","ColumnName":"t1_FirstName","DisplayName":"نام","Alliance":"t1_FirstName","Address":"c.FirstName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"ERPS.Erps.GNR3.Party","ColumnName":"t1_LastName","DisplayName":"نام خانوادگی","Alliance":"t1_LastName","Address":"c.LastName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.OrgUnit","ColumnName":"t1_DepartmentTitle","DisplayName":"واحد سازمانی","Alliance":"t1_DepartmentTitle","Address":"c.DepartmentTitle","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.OrgPost","ColumnName":"t1_PostTitle","DisplayName":"عنوان پست","Alliance":"t1_PostTitle","Address":"c.PostTitle","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_WorkLocationDesc","DisplayName":"محل خدمت","Alliance":"t1_WorkLocationDesc","Address":"c.WorkLocationDesc","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_HavayarClub","DisplayName":"باشگاه هوایاری","Alliance":"t1_HavayarClub","Address":"c.TotalDays / 365","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_EmploymentShamsiDate","DisplayName":"تاریخ استخدام","Alliance":"t1_EmploymentShamsiDate","Address":"dbo.ToShamsiDate(c.EmploymentDate, N''/'')","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_RealAnniversaryShamsiDate","DisplayName":"تاریخ سالروز حضور (واقعی)","Alliance":"t1_RealAnniversaryShamsiDate","Address":"dbo.ToShamsiDate(DATEADD(DAY, -c.TotalDays, CAST(GETDATE() AS date)), N''/'')","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_PresenceYears","DisplayName":"سال","Alliance":"t1_PresenceYears","Address":"c.TotalDays / 365","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_PresenceMonth","DisplayName":"ماه","Alliance":"t1_PresenceMonth","Address":"(c.TotalDays % 365) / 30","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_WorkExperienceYear","DisplayName":"تعداد سال سابقه کار غیر هوایاری","Alliance":"t1_WorkExperienceYear","Address":"c.WorkExperienceYear","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_WorkExperienceMonth","DisplayName":"تعداد ماه سابقه کار غیر هوایاری","Alliance":"t1_WorkExperienceMonth","Address":"c.WorkExperienceMonth","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":190,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hcm.Personel","ColumnName":"t1_BirthShamsiDate","DisplayName":"تاریخ تولد","Alliance":"t1_BirthShamsiDate","Address":"dbo.ToShamsiDate(c.BirthDate, N''/'')","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1
        THROW 51630, N'ColumnsJson / QueryJson / ActionOptions معتبر نیست.', 1;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleView)
        INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@RoleView, @Title, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = Id FROM system.SavedQuery WHERE Name = @Name;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;
        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, [Type], EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', N'[]', @EventScripts,
             1, 1, @EntityFullName, @ActionOptions,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END

    DELETE ra
    FROM system.RoleAccess ra
    WHERE ra.RowId = @ProfileId
      AND ra.ActionAccessType = 3
      AND ra.EntityName = @EntityFullName;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityFullName, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', @RoleView);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityFullName, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'ShowAllMenus', N'مشاهده کلی HTS', @RoleView)
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.RoleId = r.Id AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش سابقه حضور پرسنل","icon":"ki-calendar ki-outline","iconColor":"#a78bfa","path":"/panel/hrm/employeestatutereport/list","a_attr":{"href":"/panel/hrm/employeestatutereport/list"},"data":{"iconColor":"#a78bfa","path":"/panel/hrm/employeestatutereport/list"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51631, N'JSON برگ منو معتبر نیست.', 1;

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM system.SystemMenu
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        PRINT N'WARN: منوی AllMenus یافت نشد';
    ELSE IF CHARINDEX(N'"path":"' + @PagePath + N'"', @Content) > 0
        PRINT N'  برگ گزارش سابقه حضور پرسنل از قبل موجود است';
    ELSE
    BEGIN
        IF @Content IS NULL OR LTRIM(RTRIM(@Content)) = N'' SET @Content = N'[]';
        IF ISJSON(@Content) = 0
            THROW 51632, N'Content منوی AllMenus JSON معتبر نیست', 1;

        DECLARE @HrIdx INT = NULL;
        SELECT TOP 1 @HrIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') IN (N'منابع انسانی', N'سیستم منابع انسانی')
        ORDER BY CASE WHEN JSON_VALUE([value], '$.text') = N'منابع انسانی' THEN 0 ELSE 1 END, CAST([key] AS INT);

        IF @HrIdx IS NOT NULL
        BEGIN
            SET @Content = JSON_MODIFY(@Content,
                N'append $[' + CAST(@HrIdx AS nvarchar(10)) + N'].children',
                JSON_QUERY(@Leaf));
            PRINT N'  برگ زیر منابع انسانی اضافه شد';
        END
        ELSE
        BEGIN
            DECLARE @HrRoot NVARCHAR(MAX) = N'{"text":"منابع انسانی","icon":"ki-people ki-outline","iconColor":"#c084fc","path":"","a_attr":{"href":""},"data":{"iconColor":"#c084fc","path":""},"children":[' + @Leaf + N']}';
            SET @Content = JSON_MODIFY(@Content, N'append $', JSON_QUERY(@HrRoot));
            PRINT N'  گروه منابع انسانی به ریشه منو اضافه شد';
        END

        IF ISJSON(@Content) = 0
            THROW 51633, N'Content تولیدشده JSON معتبر نیست', 1;

        UPDATE system.SystemMenu
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName
        FROM (VALUES (@RoleView)) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName)
          AND EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m
            INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles,
            AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
    END

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL PRIMARY KEY);

        INSERT INTO #UserRoleMap (Username)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N''))
        FROM TMS.TotalSystem.dbo.Gnr_PageAction_User pau
        INNER JOIN TMS.TotalSystem.dbo.Gnr_PageAction pa ON pa.PageAction_ID = pau.PageAction_FK
        INNER JOIN TMS.TotalSystem.dbo.Gnr_User u ON u.User_ID = pau.User_FK
        WHERE pa.Page_FK = 346
          AND pa.Permission_FK IN (2, 3, 4, 5, 6, 7, 10)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @UName NVARCHAR(200), @UserId BIGINT, @AssignRoleId BIGINT;
        DECLARE @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);
        SELECT @AssignRoleId = Id FROM system.Role WHERE Name = @RoleView;

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username FROM #UserRoleMap;
        OPEN uc;
        FETCH NEXT FROM uc INTO @UName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @UserId = NULL;
            SELECT TOP 1 @UserId = u.Id
            FROM system.[User] u
            WHERE LOWER(u.Username) = LOWER(@UName)
               OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@UName);

            IF @UserId IS NULL OR @AssignRoleId IS NULL
                SET @Missing = @Missing + 1;
            ELSE
            BEGIN
                SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]')
                FROM system.[User] WHERE Id = @UserId;
                IF ISJSON(@RolesJson) <> 1 SET @RolesJson = N'[]';
                IF ISJSON(@RoleIdsJson) <> 1 SET @RoleIdsJson = N'[]';
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@RoleView))
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleView);
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @AssignRoleId)
                    SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @AssignRoleId);
                UPDATE system.[User]
                SET Roles = @RolesJson,
                    RoleIds = @RoleIdsJson,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @UserId;
                SET @Assigned = @Assigned + 1;
            END
            FETCH NEXT FROM uc INTO @UName;
        END
        CLOSE uc;
        DEALLOCATE uc;
        PRINT N'  User-role assigns touched: ' + CAST(@Assigned AS nvarchar(20)) + N', missing: ' + CAST(@Missing AS nvarchar(20));
    END
    ELSE
        PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Hrm_EmployeeStatuteReport_DataProfile ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title, EntityFullName
FROM system.SavedQuery
WHERE Name = N'Hrm_EmployeeStatuteReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Hrm_EmployeeStatuteReport';

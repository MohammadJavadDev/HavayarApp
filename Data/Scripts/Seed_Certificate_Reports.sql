-- ============================================
-- SavedQuery + ReportBuilderReport قالب‌های گواهی
-- Cng و Certificate1..Certificate10
-- پارامترها: @certificateId (چاپ تکی) و @courseId (چاپ همه)
-- Idempotent
-- Apply with Seed_Certificate_Reports.ps1 (UTF-8). sqlcmd without -f 65001 corrupts Persian.
-- ============================================

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-certificate-reports';
DECLARE @Name NVARCHAR(200) = N'trn_certificate';
DECLARE @Title NVARCHAR(200) = N'داده گواهی‌نامه';

DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    c.Id AS CertificateId,
    c.CertificateNo,
    c.IssueShamsiDate,
    c.TemplateKey,
    CASE WHEN c.CourseParticipantId IS NOT NULL THEN N''عمومی'' ELSE N''اختصاصی'' END AS EnrollmentKind,
    LTRIM(RTRIM(ISNULL(p.FirstName, N'''') + N'' '' + ISNULL(p.LastName, N''''))) AS FullName,
    CASE WHEN p.Gender = 695 THEN N''جناب آقای '' ELSE N''سرکار خانم '' END
        + LTRIM(RTRIM(ISNULL(p.FirstName, N'''') + N'' '' + ISNULL(p.LastName, N'''')))
        AS FullNameHonorific,
    CASE WHEN p.Gender = 695 THEN N''Mr.'' ELSE N''Ms.'' END
        + LTRIM(RTRIM(ISNULL(NULLIF(LTRIM(RTRIM(p.FirstNameInLatin)), N''''), p.FirstName)))
        + N'' ''
        + LTRIM(RTRIM(ISNULL(NULLIF(LTRIM(RTRIM(p.LastNameInLatin)), N''''), p.LastName)))
        AS FullNameLatin,
    p.FirstName, p.LastName, p.FirstNameInLatin, p.LastNameInLatin, p.FatherName, p.NationalCode, p.BirthdayYear,
    CASE p.Gender WHEN 695 THEN N''آقا'' WHEN 696 THEN N''خانم'' ELSE N'''' END AS GenderTitle,
    p.Mobile, p.Email,
    co.TrainingCode,
    cb.CourseFieldTitle,
    cb.CourseFieldTitleInLatin,
    CONVERT(varchar(10), ISNULL(c.IssueMiladiDate, c.CreatedOnMiladiDateTime), 105) AS IssueDateEn,
    CASE
        WHEN co.StartMiladiDate IS NULL OR co.EndMiladiDate IS NULL THEN NULL
        WHEN DAY(co.StartMiladiDate) = DAY(co.EndMiladiDate)
            THEN CONCAT(CAST(DAY(co.EndMiladiDate) AS nvarchar(2)), FORMAT(co.EndMiladiDate, '' MMMM yyyy'', ''en-US''))
        ELSE CONCAT(
                CAST(DAY(co.StartMiladiDate) AS nvarchar(2)),
                CASE WHEN FORMAT(co.StartMiladiDate, ''MMMM'', ''en-US'') <> FORMAT(co.EndMiladiDate, ''MMMM'', ''en-US'')
                     THEN CONCAT(N'' '', FORMAT(co.StartMiladiDate, ''MMMM'', ''en-US'')) ELSE N'''' END,
                N'' - '',
                CAST(DAY(co.EndMiladiDate) AS nvarchar(2)),
                FORMAT(co.EndMiladiDate, '' MMMM yyyy'', ''en-US''),
                N'' '',
                CASE WHEN co.Days IS NOT NULL AND (co.Days > 2 OR DATEDIFF(day, co.StartMiladiDate, co.EndMiladiDate) + 1 > co.Days)
                     THEN CONCAT(N''(In '', CAST(co.Days AS nvarchar(10)), N'' days)'') ELSE N'''' END)
    END AS DurationDate,
    co.StartShamsiDate, co.EndShamsiDate,
    co.DurationInMinute,
    cb.Duration AS DurationHours,
    COALESCE(cp.CngScore, CAST(ccp.Score AS smallint)) AS CngScore,
    CASE WHEN COALESCE(cp.CngScore, ccp.Score) IS NULL THEN NULL
         ELSE N''with the score of '' + CAST(COALESCE(cp.CngScore, ccp.Score) AS nvarchar(5)) + N'' out of 100''
    END AS ScoreString,
    CASE WHEN cb.CourseFieldTitle LIKE N''%تکنسینی%'' THEN N''صرف داشتن این گواهینامه به منزله داشتن توانایی در انجام نگهداری و تعمیرات تجهیزات گروه صنعتی هوایار نمی باشد.'' ELSE NULL END AS Explain,
    NULLIF(LTRIM(RTRIM(photo.PhysicalPath)), N'''') AS PhotoUrl,
    LTRIM(RTRIM(ISNULL(t.FirstName, N'''') + N'' '' + ISNULL(t.LastName, N''''))) AS TeacherName,
    COALESCE(cpComp.Title, pComp.Title, coComp.Title) AS CompanyTitle,
    NULLIF(LTRIM(RTRIM(COALESCE(cpComp.Description, coComp.Description, pComp.Description))), N'''') AS CompanyTitleInLatin,
    co.TrainingCourseTitles, co.TrainingCourseTitles1, co.TrainingCourseTitles2, co.TrainingCourseTitles3
FROM Trn.Certificate AS c
LEFT JOIN Trn.CourseParticipant AS cp ON cp.Id = c.CourseParticipantId
LEFT JOIN Trn.CourseCompanyParticipant AS ccp ON ccp.Id = c.CourseCompanyParticipantId
LEFT JOIN Trn.Participant AS p ON p.Id = COALESCE(cp.ParticipantId, ccp.ParticipantId)
LEFT JOIN dbo.FileEntity AS photo ON photo.Id = p.PhotoFileId
LEFT JOIN Trn.Course AS co ON co.Id = COALESCE(cp.CourseId, ccp.CourseId)
LEFT JOIN Trn.CourseBank AS cb ON cb.Id = co.CourseBankId
LEFT JOIN Trn.TeacherBank AS t ON t.Id = co.TeacherId
LEFT JOIN Trn.Company AS cpComp ON cpComp.Id = cp.CompanyId
LEFT JOIN Trn.Company AS pComp ON pComp.Id = p.CompanyId
LEFT JOIN Trn.Company AS coComp ON coComp.Id = co.CompanyId
WHERE (
    TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @certificateId))), N'''')) IS NULL
    OR c.Id = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @certificateId))), N''''))
)
AND (
    TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @courseId))), N'''')) IS NULL
    OR COALESCE(cp.CourseId, ccp.CourseId) = TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @courseId))), N''''))
)
AND (
    TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @certificateId))), N'''')) IS NOT NULL
    OR TRY_CONVERT(bigint, NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(50), @courseId))), N'''')) IS NOT NULL
)';

DECLARE @QueryJson NVARCHAR(MAX) = N'{
  "Tables":[],
  "Relations":[],
  "Filters":[],
  "CustomConditions":null,
  "CustomQuery":null,
  "Parameters":[
    {"Id":"certificateId","Name":"certificateId","DisplayName":"شناسه گواهی","DefaultValue":"","DataType":"int"},
    {"Id":"courseId","Name":"courseId","DisplayName":"شناسه دوره","DefaultValue":"","DataType":"int"}
  ],
  "Selects":[{"Index":0,"Name":"Report","Title":"Report"}]
}';
SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":null,"ColumnName":"CertificateId","DisplayName":"شناسه گواهی","Alliance":"CertificateId","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CertificateNo","DisplayName":"شماره گواهی","Alliance":"CertificateNo","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"IssueShamsiDate","DisplayName":"تاریخ صدور","Alliance":"IssueShamsiDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"TemplateKey","DisplayName":"قالب","Alliance":"TemplateKey","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"EnrollmentKind","DisplayName":"مسیر","Alliance":"EnrollmentKind","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FullName","DisplayName":"نام کامل","Alliance":"FullName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FullNameHonorific","DisplayName":"نام با عنوان","Alliance":"FullNameHonorific","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FullNameLatin","DisplayName":"نام لاتین","Alliance":"FullNameLatin","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FirstName","DisplayName":"نام","Alliance":"FirstName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"LastName","DisplayName":"نام خانوادگی","Alliance":"LastName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FirstNameInLatin","DisplayName":"نام لاتین","Alliance":"FirstNameInLatin","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"LastNameInLatin","DisplayName":"نام خانوادگی لاتین","Alliance":"LastNameInLatin","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"FatherName","DisplayName":"نام پدر","Alliance":"FatherName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"NationalCode","DisplayName":"کد ملی","Alliance":"NationalCode","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"BirthdayYear","DisplayName":"سال تولد","Alliance":"BirthdayYear","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"GenderTitle","DisplayName":"جنسیت","Alliance":"GenderTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"Mobile","DisplayName":"موبایل","Alliance":"Mobile","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"Email","DisplayName":"ایمیل","Alliance":"Email","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"TrainingCode","DisplayName":"کد آموزش","Alliance":"TrainingCode","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CourseFieldTitle","DisplayName":"عنوان دوره","Alliance":"CourseFieldTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CourseFieldTitleInLatin","DisplayName":"عنوان دوره لاتین","Alliance":"CourseFieldTitleInLatin","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"IssueDateEn","DisplayName":"تاریخ صدور لاتین","Alliance":"IssueDateEn","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"DurationDate","DisplayName":"بازه تاریخ لاتین","Alliance":"DurationDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"StartShamsiDate","DisplayName":"شروع","Alliance":"StartShamsiDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"EndShamsiDate","DisplayName":"پایان","Alliance":"EndShamsiDate","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"DurationInMinute","DisplayName":"مدت (دقیقه)","Alliance":"DurationInMinute","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"DurationHours","DisplayName":"مدت (ساعت)","Alliance":"DurationHours","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CngScore","DisplayName":"نمره CNG","Alliance":"CngScore","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"ScoreString","DisplayName":"متن نمره لاتین","Alliance":"ScoreString","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":null},
{"TableName":null,"ColumnName":"Explain","DisplayName":"توضیح گواهی","Alliance":"Explain","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"PhotoUrl","DisplayName":"عکس","Alliance":"PhotoUrl","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":null},
{"TableName":null,"ColumnName":"TeacherName","DisplayName":"مدرس","Alliance":"TeacherName","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CompanyTitle","DisplayName":"شرکت","Alliance":"CompanyTitle","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"CompanyTitleInLatin","DisplayName":"شرکت لاتین","Alliance":"CompanyTitleInLatin","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":null},
{"TableName":null,"ColumnName":"TrainingCourseTitles","DisplayName":"عنوان دوره 0","Alliance":"TrainingCourseTitles","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":null},
{"TableName":null,"ColumnName":"TrainingCourseTitles1","DisplayName":"عنوان دوره 1","Alliance":"TrainingCourseTitles1","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":null},
{"TableName":null,"ColumnName":"TrainingCourseTitles2","DisplayName":"عنوان دوره 2","Alliance":"TrainingCourseTitles2","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":null},
{"TableName":null,"ColumnName":"TrainingCourseTitles3","DisplayName":"عنوان دوره 3","Alliance":"TrainingCourseTitles3","Address":null,"SystemTypeName":null,"SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":false,"Sortable":false,"ClassName":null}
]';

DECLARE @SavedQueryId BIGINT;

IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
BEGIN
    UPDATE [system].[SavedQuery]
    SET Title = @Title,
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        EntityFullName = N'entities.app.trn.certificate',
        Mode = 1,
        Type = 0,
        ActionOptions = N'',
        CustomActionButtonsJson = N'[]',
        EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
        DiagramJson = N'[]',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi,
        IsActive = 1
    WHERE Name = @Name;

    SELECT @SavedQueryId = Id FROM [system].[SavedQuery] WHERE Name = @Name;
END
ELSE
BEGIN
    INSERT INTO [system].[SavedQuery]
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
        @Name, @Title, @QueryJson, @ColumnsJson, N'[]',
        N'[]', N'{"onSelectedRow":"","onRowAdded":""}',
        1, 0, N'entities.app.trn.certificate', N'',
        1, 1, @SeedUser, @SeedUser,
        @Now, @NowShamsi, @Now, @NowShamsi,
        1
    );

    SET @SavedQueryId = SCOPE_IDENTITY();
END;

UPDATE dbo.ReportBuilderReport
SET BaseQuery = @CustomQuery,
    SavedQueryId = @SavedQueryId,
    Type = 1,
    ObjectType = N'Table',
    ModifiedById = 1,
    ModifiedByName = @SeedUser,
    ModifiedDateMiladiDateTime = @Now,
    ModifiedDateShamsiDateTime = @NowShamsi
WHERE Name IN (
    N'Cng', N'Certificate1', N'Certificate2', N'Certificate3', N'Certificate4',
    N'Certificate5', N'Certificate6', N'Certificate7', N'Certificate8',
    N'Certificate9', N'Certificate10'
);

SELECT @SavedQueryId AS SavedQueryId, @Name AS QueryName,
       (SELECT COUNT(*) FROM dbo.ReportBuilderReport WHERE Name LIKE N'Certificate%' OR Name = N'Cng') AS CertificateReportCount;

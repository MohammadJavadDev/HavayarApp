/*
نمایه داده، دسترسی و منوی شکایات مشتری.
اعمال: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CrmCustomerComplaint_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = SYSDATETIME();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-crm-complaint';
    DECLARE @Name NVARCHAR(200) = N'Crm_CustomerComplaint_List';
    DECLARE @Title NVARCHAR(200) = N'شکایات مشتری';
    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @CurrentUserId) AS BIGINT);
DECLARE @Roles NVARCHAR(MAX) = N''[]'';
SELECT @Roles = CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N''[]'' END
FROM [system].[User] u WHERE u.Id = @CuId;
DECLARE @OwnOnly BIT = 0;
IF EXISTS (SELECT 1 FROM OPENJSON(@Roles) j WHERE j.[value] = N''Crm.Complaint.OwnItems'')
   AND NOT EXISTS (SELECT 1 FROM OPENJSON(@Roles) j WHERE j.[value] IN (N''Crm.Complaint.Manager'', N''Crm.Complaint.Expert''))
    SET @OwnOnly = 1;
--!--mainsection
SELECT
    t1.Id AS t1_Id,
    CASE t1.OwnerType WHEN 1499 THEN N''هوایاری'' WHEN 1500 THEN N''غیر هوایاری'' ELSE N'''' END AS t1_OwnerType,
    cust.Code AS t2_Code,
    party.FullName AS t2_FullName,
    CASE t1.CustomerType WHEN 106 THEN N''حقیقی'' WHEN 107 THEN N''حقوقی'' ELSE N'''' END AS t1_CustomerType,
    t1.CustomerTitle AS t1_CustomerTitle,
    t1.SerialNumber AS t1_SerialNumber,
    t1.CompletionShamsiDate AS t1_CompletionShamsiDate,
    CASE WHEN EXISTS (SELECT 1 FROM Crm.CustomerComplaintAttachment a WHERE a.ComplaintId = t1.Id) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS t1_HasAttachment,
    contact.PrePhoneNumber AS t3_PrePhoneNumber,
    contact.PhoneNumber AS t3_PhoneNumber,
    contact.MobileNumber AS t3_MobileNumber,
    contact.ResponserFullName AS t3_ResponserFullName,
    contact.ResponserJob AS t3_ResponserJob,
    t1.Address AS t1_Address,
    t1.ComplaintReceiver AS t1_ComplaintReceiver,
    t1.ComplaintPresenter AS t1_ComplaintPresenter,
    t1.OrganizationUnitInText AS t1_OrganizationUnitInText,
    t1.ExpertOrManagerInText AS t1_ExpertOrManagerInText,
    CASE t1.Priority WHEN 1122 THEN N''فوری'' WHEN 1123 THEN N''غیر فوری'' ELSE N'''' END AS t1_Priority,
    t1.TrackingCode AS t1_TrackingCode,
    channel.Title AS t4_Title,
    CASE t1.GeneralComplaintSubject WHEN 1495 THEN N''محصول'' WHEN 1496 THEN N''فرآیند'' WHEN 1497 THEN N''سایر'' WHEN 1775 THEN N''محصول - فرآیند'' ELSE N'''' END AS t1_GeneralComplaintSubject,
    t1.GeneralComplaintSubjectTitle AS t1_GeneralComplaintSubjectTitle,
    CASE t1.ComplaintType WHEN 1531 THEN N''بحرانی'' WHEN 1532 THEN N''بسیار مهم'' WHEN 1533 THEN N''مهم'' WHEN 2739 THEN N''سایر'' ELSE N'''' END AS t1_ComplaintType,
    t1.RaisedSubjects AS t1_RaisedSubjects,
    CASE t1.ComplaintMainSubject
        WHEN 2551 THEN N''رد شدن در ارزیابی کیفی'' WHEN 2552 THEN N''عدم تأیید پیشنهاد فنی'' WHEN 2553 THEN N''عدم تأیید پیشنهاد مالی''
        WHEN 2554 THEN N''تاخیر بازرگانی'' WHEN 2555 THEN N''تاخیر تدارکات'' WHEN 2556 THEN N''تاخیر مالی'' WHEN 2557 THEN N''تاخیر خدمات''
        WHEN 2558 THEN N''داشتن الزام ساخت داخل'' WHEN 2559 THEN N''ابطال مناقصه'' WHEN 2560 THEN N''قیمت قطعه''
        WHEN 2561 THEN N''کیفیت خدمات'' WHEN 2562 THEN N''کیفیت فروش'' WHEN 2563 THEN N''کیفیت قطعه'' WHEN 2564 THEN N''کیفیت محصول''
        ELSE ISNULL(t1.MainSubjectsInText, N'''') END AS t1_ComplaintMainSubject,
    t1.Repeatability AS t1_Repeatability,
    t1.HasFinancialLoad AS t1_HasFinancialLoad,
    t1.HasUrgencyHandling AS t1_HasUrgencyHandling,
    t1.HasComplexity AS t1_HasComplexity,
    inform.Title AS t5_Title,
    CASE t1.ComplaintStatus
        WHEN 1168 THEN N''دریافت شکایت'' WHEN 1169 THEN N''رسیدگی اولیه'' WHEN 1170 THEN N''تکمیل اطلاعات و رسیدگی نهایی''
        WHEN 1171 THEN N''اعلام نتیجه به مشتری'' WHEN 1172 THEN N''عدم جلب رضایت و بسته'' WHEN 1173 THEN N''جلب رضایت و بسته''
        ELSE N'''' END AS t1_ComplaintStatus,
    t1.HandlingResultIdsInText AS t1_HandlingResultIdsInText,
    CASE t1.InformComplainant WHEN 1178 THEN N''انجام شد'' WHEN 1179 THEN N''انجام نشد'' ELSE N'''' END AS t1_InformComplainant,
    CASE t1.ComplaintConsent WHEN 1180 THEN N''حاصل شد'' WHEN 1181 THEN N''حاصل نشد'' ELSE N'''' END AS t1_ComplaintConsent,
    CASE t1.FinalComplaintStatus WHEN 1182 THEN N''بسته شد'' WHEN 1183 THEN N''بسته نشد'' ELSE N'''' END AS t1_FinalComplaintStatus,
    CAST(t1.FinalComplaintStatus AS int) AS t1_FinalStatusValue,
    t1.ComplaintClosingShamsiDate AS t1_ComplaintClosingShamsiDate,
    CASE WHEN t1.ComplaintClosingMiladiDate IS NULL OR t1.CompletionMiladiDate IS NULL THEN 0
         ELSE DATEDIFF(DAY, t1.CompletionMiladiDate, t1.ComplaintClosingMiladiDate) END AS t1_CloseDurationInDay,
    t1.DelayReason AS t1_DelayReason,
    nxt.NextActionShamsiDate AS t6_NextActionShamsiDate,
    t1.CreatedOnShamsiDateTime AS t1_CreatedOnShamsiDateTime,
    t1.ModifiedDateShamsiDateTime AS t1_ModifiedDateShamsiDateTime
FROM Crm.CustomerComplaint AS t1
LEFT JOIN SLS.Customer AS cust ON cust.Id = t1.CustomerId
LEFT JOIN Gnr.Party AS party ON party.Id = cust.PartyId
LEFT JOIN Crm.ComplaintLookupItem AS channel ON channel.Id = t1.CommunicationChannelId
LEFT JOIN Crm.ComplaintLookupItem AS inform ON inform.Id = t1.InformForReceiveComplaintId
OUTER APPLY (
    SELECT TOP (1) c.PrePhoneNumber, c.PhoneNumber, c.MobileNumber, c.ResponserFullName, c.ResponserJob
    FROM Crm.CustomerComplaintContact AS c
    WHERE c.ComplaintId = t1.Id
    ORDER BY c.Id
) AS contact
OUTER APPLY (
    SELECT TOP (1) a.NextActionShamsiDate
    FROM Crm.CustomerComplaintAction AS a
    WHERE a.ComplaintId = t1.Id
    ORDER BY a.NextActionMiladiDate DESC, a.Id DESC
) AS nxt
WHERE t1.ModuleTypeId = 1691
  AND (
        @OwnOnly = 0
        OR (N'','' + ISNULL(t1.ViewersIds, N'''') + N'','') LIKE N''%,'' + CONVERT(nvarchar(20), @CuId) + N'',%''
      )';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1_Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_OwnerType","DisplayName":"نوع","Alliance":"t1_OwnerType","Address":"[t1_OwnerType]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t2_Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2_Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t2_FullName","DisplayName":"نام مشتری","Alliance":"t2_FullName","Address":"[t2_FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":230,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_CustomerType","DisplayName":"نوع مشتری","Alliance":"t1_CustomerType","Address":"[t1_CustomerType]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_CustomerTitle","DisplayName":"عنوان مشتری","Alliance":"t1_CustomerTitle","Address":"[t1_CustomerTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_SerialNumber","DisplayName":"شماره سریال","Alliance":"t1_SerialNumber","Address":"[t1_SerialNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_CompletionShamsiDate","DisplayName":"تاریخ تکمیل","Alliance":"t1_CompletionShamsiDate","Address":"[t1_CompletionShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_HasAttachment","DisplayName":"پیوست","Alliance":"t1_HasAttachment","Address":"[t1_HasAttachment]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t3_PrePhoneNumber","DisplayName":"پیش‌شماره","Alliance":"t3_PrePhoneNumber","Address":"[t3_PrePhoneNumber]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t3_PhoneNumber","DisplayName":"تلفن","Alliance":"t3_PhoneNumber","Address":"[t3_PhoneNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t3_MobileNumber","DisplayName":"موبایل","Alliance":"t3_MobileNumber","Address":"[t3_MobileNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t3_ResponserFullName","DisplayName":"نام پاسخگو","Alliance":"t3_ResponserFullName","Address":"[t3_ResponserFullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t3_ResponserJob","DisplayName":"سمت پاسخگو","Alliance":"t3_ResponserJob","Address":"[t3_ResponserJob]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_Address","DisplayName":"آدرس","Alliance":"t1_Address","Address":"[t1_Address]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintReceiver","DisplayName":"دریافت کننده","Alliance":"t1_ComplaintReceiver","Address":"[t1_ComplaintReceiver]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintPresenter","DisplayName":"طرح کننده","Alliance":"t1_ComplaintPresenter","Address":"[t1_ComplaintPresenter]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_OrganizationUnitInText","DisplayName":"واحد سازمانی","Alliance":"t1_OrganizationUnitInText","Address":"[t1_OrganizationUnitInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ExpertOrManagerInText","DisplayName":"کارشناس یا مدیر","Alliance":"t1_ExpertOrManagerInText","Address":"[t1_ExpertOrManagerInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_Priority","DisplayName":"اولویت","Alliance":"t1_Priority","Address":"[t1_Priority]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_TrackingCode","DisplayName":"کد رهگیری","Alliance":"t1_TrackingCode","Address":"[t1_TrackingCode]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t4_Title","DisplayName":"کانال ارتباط","Alliance":"t4_Title","Address":"[t4_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_GeneralComplaintSubject","DisplayName":"موضوع کلی","Alliance":"t1_GeneralComplaintSubject","Address":"[t1_GeneralComplaintSubject]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_GeneralComplaintSubjectTitle","DisplayName":"عنوان موضوع کلی","Alliance":"t1_GeneralComplaintSubjectTitle","Address":"[t1_GeneralComplaintSubjectTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintType","DisplayName":"نوع شکایت","Alliance":"t1_ComplaintType","Address":"[t1_ComplaintType]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_RaisedSubjects","DisplayName":"موضوعات مطرح شده","Alliance":"t1_RaisedSubjects","Address":"[t1_RaisedSubjects]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintMainSubject","DisplayName":"موضوع اصلی","Alliance":"t1_ComplaintMainSubject","Address":"[t1_ComplaintMainSubject]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_Repeatability","DisplayName":"تکرارپذیری","Alliance":"t1_Repeatability","Address":"[t1_Repeatability]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_HasFinancialLoad","DisplayName":"بار مالی","Alliance":"t1_HasFinancialLoad","Address":"[t1_HasFinancialLoad]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_HasUrgencyHandling","DisplayName":"فوریت رسیدگی","Alliance":"t1_HasUrgencyHandling","Address":"[t1_HasUrgencyHandling]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_HasComplexity","DisplayName":"پیچیدگی","Alliance":"t1_HasComplexity","Address":"[t1_HasComplexity]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t5_Title","DisplayName":"اطلاع از دریافت شکایت","Alliance":"t5_Title","Address":"[t5_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintStatus","DisplayName":"وضعیت شکایت","Alliance":"t1_ComplaintStatus","Address":"[t1_ComplaintStatus]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_HandlingResultIdsInText","DisplayName":"نتیجه رسیدگی","Alliance":"t1_HandlingResultIdsInText","Address":"[t1_HandlingResultIdsInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_InformComplainant","DisplayName":"اطلاع به شاکی","Alliance":"t1_InformComplainant","Address":"[t1_InformComplainant]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintConsent","DisplayName":"رضایت شاکی","Alliance":"t1_ComplaintConsent","Address":"[t1_ComplaintConsent]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_FinalComplaintStatus","DisplayName":"وضعیت نهایی","Alliance":"t1_FinalComplaintStatus","Address":"[t1_FinalComplaintStatus]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_FinalStatusValue","DisplayName":"کد وضعیت نهایی","Alliance":"t1_FinalStatusValue","Address":"[t1_FinalStatusValue]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ComplaintClosingShamsiDate","DisplayName":"تاریخ بستن","Alliance":"t1_ComplaintClosingShamsiDate","Address":"[t1_ComplaintClosingShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_CloseDurationInDay","DisplayName":"مدت بستن (روز)","Alliance":"t1_CloseDurationInDay","Address":"[t1_CloseDurationInDay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_DelayReason","DisplayName":"علت تاخیر","Alliance":"t1_DelayReason","Address":"[t1_DelayReason]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t6_NextActionShamsiDate","DisplayName":"تاریخ اقدام بعدی","Alliance":"t6_NextActionShamsiDate","Address":"[t6_NextActionShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1_CreatedOnShamsiDateTime]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.CustomerComplaint","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1_ModifiedDateShamsiDateTime]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @Buttons NVARCHAR(MAX) = N'[{"id":"cc_attachment","title":"پیوست","dataActionName":"cc_attachment","colorClass":"btn-color-primary","iconClass":"ki-paper-clip ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n  var row = ctx.selectedRow || {};\n  var id = ctx.primaryKeyValue || row.t1_Id;\n  if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n  appController.addPage(''/Panel/Crm/CustomerComplaintAttachment/ListByParentId?complaintId='' + id, true, ''پیوست شکایت'');\n}"},{"id":"cc_print","title":"چاپ","dataActionName":"cc_print","colorClass":"btn-color-info","iconClass":"ki-printer ki-outline","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n  Swal.fire({\n    title: ''چاپ'',\n    html: ''<div class=\"text-end\"><label>از تاریخ</label><input id=\"ccFrom\" class=\"form-control mb-2\" placeholder=\"1404/01/01\" /><label>تا تاریخ</label><input id=\"ccTo\" class=\"form-control\" placeholder=\"1404/12/29\" /></div>'',\n    showCancelButton: true,\n    confirmButtonText: ''چاپ'',\n    cancelButtonText: ''انصراف'',\n    preConfirm: function () {\n      var from = document.getElementById(''ccFrom'').value;\n      var to = document.getElementById(''ccTo'').value;\n      if (!from || !to) { Swal.showValidationMessage(''از تاریخ و تا تاریخ را وارد کنید''); return false; }\n      return { from: from, to: to };\n    }\n  }).then(function (r) {\n    if (!r.isConfirmed || !r.value) return;\n    var q = ''fromDate={\\''values\\'':[\\'''' + r.value.from + ''\\''],\\''condition\\'':\\''=\\''}&toDate={\\''values\\'':[\\'''' + r.value.to + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'';\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/CustomerComplaintSummary?'' + q);\n  });\n}"},{"id":"cc_print_actions","title":"چاپ اقدامات انجام شده","dataActionName":"cc_print_actions","colorClass":"btn-color-info","iconClass":"ki-printer ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n  var row = ctx.selectedRow || {};\n  var id = ctx.primaryKeyValue || row.t1_Id;\n  if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n  appController.addPage(''/System/ReportBuilder/ViewReportByName/CustomerComplaintActions?complaintId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"},{"id":"cc_sms_receipt","title":"ارسال پیام ثبت شکایت","dataActionName":"cc_sms_receipt","colorClass":"btn-color-success","iconClass":"ki-sms ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n  var row = ctx.selectedRow || {};\n  var id = ctx.primaryKeyValue || row.t1_Id;\n  var status = String(row.t1_FinalStatusValue == null ? '''' : row.t1_FinalStatusValue);\n  if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n  if (status !== ''1183'') { toastr.warning(''ارسال پیام ثبت شکایت فقط وقتی وضعیت نهایی بسته نشد است''); return; }\n  Swal.fire({ title: ''ارسال پیام ثبت شکایت'', icon: ''warning'', showCancelButton: true, confirmButtonText: ''ارسال'', cancelButtonText: ''خیر'' }).then(function (r) {\n    if (!r.isConfirmed) return;\n    post(''/Panel/Crm/CustomerComplaint/SendReceiptSms?id='' + id, {}, function (resp) {\n      if (!resp.isSuccess) { toastr.error(resp.message || ''خطا'', ''خطا''); return; }\n      toastr.success(''ارسال پیامک با موفقیت انجام شد'');\n    });\n  });\n}"},{"id":"cc_sms_closed","title":"ارسال پیام پایان شکایت","dataActionName":"cc_sms_closed","colorClass":"btn-color-success","iconClass":"ki-sms ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n  var row = ctx.selectedRow || {};\n  var id = ctx.primaryKeyValue || row.t1_Id;\n  var status = String(row.t1_FinalStatusValue == null ? '''' : row.t1_FinalStatusValue);\n  if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n  if (status !== ''1182'') { toastr.warning(''ارسال پیام پایان شکایت فقط وقتی وضعیت نهایی بسته شد است''); return; }\n  Swal.fire({ title: ''ارسال پیام پایان شکایت'', icon: ''warning'', showCancelButton: true, confirmButtonText: ''ارسال'', cancelButtonText: ''خیر'' }).then(function (r) {\n    if (!r.isConfirmed) return;\n    post(''/Panel/Crm/CustomerComplaint/SendClosedSms?id='' + id, {}, function (resp) {\n      if (!resp.isSuccess) { toastr.error(resp.message || ''خطا'', ''خطا''); return; }\n      toastr.success(''ارسال پیامک با موفقیت انجام شد'');\n    });\n  });\n}"}]';
    DECLARE @Actions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @Events NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Id BIGINT;

    IF EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = @Name)
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title, QueryJson = @QueryJson, ColumnsJson = @ColumnsJson,
            CustomActionButtonsJson = @Buttons, EventScriptsJson = @Events, ActionOptions = @Actions,
            EntityFullName = N'entities.app.crm.customercomplaint', Mode = 1, Type = 1, DiagramJson = N'{}',
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @Name;
        SELECT @Id = Id FROM [system].[SavedQuery] WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @Buttons, @Events,
             1, 1, N'entities.app.crm.customercomplaint', @Actions,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Id = SCOPE_IDENTITY();
    END

    DECLARE @Roles TABLE (Name NVARCHAR(200), Title NVARCHAR(200));
    INSERT INTO @Roles (Name, Title) VALUES
        (N'Crm.Complaint.Manager', N'مدیر سیستم ارتباط با مشتریان'),
        (N'Crm.Complaint.Expert', N'کارشناسان شکایات مشتریان'),
        (N'Crm.Complaint.Read', N'دسترسی مشاهده سیستم ارتباط با مشتری'),
        (N'Crm.Complaint.Observer', N'مشاهده شکایات مشتری'),
        (N'Crm.Complaint.OwnItems', N'مشاهده شکایت‌های خود');

    INSERT INTO [system].[Role]
        (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT r.Name, r.Title, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM @Roles r
    WHERE NOT EXISTS (SELECT 1 FROM [system].[Role] x WHERE x.Name = r.Name);

    DELETE ra FROM [system].[RoleAccess] ra
    INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
    WHERE r.Name LIKE N'Crm.Complaint.%'
      AND (ra.ActionAccessType = 3 OR ra.Path LIKE N'/panel/crm/customercomplaint%');

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Id AS nvarchar(20)), 3, 7, N'Entities.App.Crm.CustomerComplaint', @Title, @Id, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name LIKE N'Crm.Complaint.%';

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT g.Path, g.AType, g.IType, N'Entities.App.Crm.CustomerComplaint', r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/list', 1, 1),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/edit', 1, 5),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/new', 1, 4),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/save', 2, 3),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/add', 2, 4),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/update', 2, 5),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/delete', 2, 6),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/exporttoexcel', 2, 1000),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/save', 2, 3),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/delete', 2, 6),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/edit', 1, 5),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/new', 1, 4),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/save', 2, 3),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/delete', 2, 6),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/edit', 1, 5),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/new', 1, 4),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/save', 2, 3),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/delete', 2, 6),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/edit', 1, 5),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/new', 1, 4),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/listbyparentid', 1, 1),
    (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/list', 1, 1),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/edit', 1, 5),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/new', 1, 4),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/save', 2, 3),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/add', 2, 4),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/update', 2, 5),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/delete', 2, 6),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintcontact/save', 2, 3),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintcontact/delete', 2, 6),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintcontact/edit', 1, 5),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintcontact/new', 1, 4),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintcontact/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintaction/save', 2, 3),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintaction/delete', 2, 6),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintaction/edit', 1, 5),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintaction/new', 1, 4),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintaction/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/save', 2, 3),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/delete', 2, 6),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/edit', 1, 5),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/new', 1, 4),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/listbyparentid', 1, 1),
    (N'Crm.Complaint.Expert', N'/panel/crm/customercomplaintattachment/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaint/list', 1, 1),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaint/exporttoexcel', 2, 1000),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/save', 2, 3),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/delete', 2, 6),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/edit', 1, 5),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/new', 1, 4),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/listbyparentid', 1, 1),
    (N'Crm.Complaint.Read', N'/panel/crm/customercomplaintattachment/getlistbyparentid', 2, 2),
    (N'Crm.Complaint.Observer', N'/panel/crm/customercomplaint/list', 1, 1),
    (N'Crm.Complaint.Observer', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
    (N'Crm.Complaint.Observer', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
    (N'Crm.Complaint.Observer', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000),
    (N'Crm.Complaint.OwnItems', N'/panel/crm/customercomplaint/list', 1, 1),
    (N'Crm.Complaint.OwnItems', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
    (N'Crm.Complaint.OwnItems', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
    (N'Crm.Complaint.OwnItems', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000)
    ) g(RoleName, Path, AType, IType)
    INNER JOIN [system].[Role] r ON r.Name = g.RoleName;

    DECLARE @Menu NVARCHAR(MAX) = N'[{"text":"عملیات","icon":"ki-setting-2 ki-outline","iconColor":"#3699ff","path":"","children":[{"text":"شکایات مشتری","icon":"ki-message-text ki-outline","iconColor":"#d78819","path":"/panel/crm/customercomplaint/list","a_attr":{"href":"/panel/crm/customercomplaint/list"},"data":{"iconColor":"#d78819","path":"/panel/crm/customercomplaint/list"},"children":[]}]}]';
    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'CrmSystem')
        UPDATE [system].[SystemMenu]
        SET Title = N'سیستم ارتباط با مشتری', Content = @Menu, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'CrmSystem';
    ELSE
        INSERT INTO [system].[SystemMenu]
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (N'سیستم ارتباط با مشتری', N'CrmSystem', @Menu, N'[]', N'[]',
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    DECLARE @RoleIds NVARCHAR(MAX) = (
        SELECT N'[' + STRING_AGG(CAST(Id AS nvarchar(20)), N',') + N']'
        FROM [system].[Role] WHERE Name LIKE N'Crm.Complaint.%');
    DECLARE @RoleNames NVARCHAR(MAX) = (
        SELECT N'[' + STRING_AGG(N'"' + STRING_ESCAPE(Name, 'json') + N'"', N',') + N']'
        FROM [system].[Role] WHERE Name LIKE N'Crm.Complaint.%');
    UPDATE [system].[SystemMenu]
    SET AccessRoleIds = @RoleIds, AccessRoles = @RoleNames
    WHERE Name = N'CrmSystem';

    /* همه منوها: ارتباط با مشتری / عملیات / شکایات مشتری */
    DECLARE @AllItem NVARCHAR(MAX) = N'{"text":"شکایات مشتری","icon":"ki-message-text ki-outline","iconColor":"#6EE7B7","path":"/panel/crm/customercomplaint/list","a_attr":{"href":"/panel/crm/customercomplaint/list"},"data":{"iconColor":"#6EE7B7","path":"/panel/crm/customercomplaint/list"},"children":[]}';
    DECLARE @CrmOrd INT = (
        SELECT TOP 1 TRY_CAST(r.[key] AS int)
        FROM [system].[SystemMenu] m
        CROSS APPLY OPENJSON(m.Content) r
        WHERE m.Name = N'AllMenus' AND JSON_VALUE(r.value, '$.text') = N'ارتباط با مشتری');
    DECLARE @OpOrd INT = (
        SELECT TOP 1 TRY_CAST(c.[key] AS int)
        FROM [system].[SystemMenu] m
        CROSS APPLY OPENJSON(m.Content, CONCAT('$[', @CrmOrd, '].children')) c
        WHERE m.Name = N'AllMenus' AND JSON_VALUE(c.value, '$.text') = N'عملیات');
    IF @CrmOrd IS NOT NULL AND @OpOrd IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM [system].[SystemMenu] m
            CROSS APPLY OPENJSON(m.Content, CONCAT('$[', @CrmOrd, '].children[', @OpOrd, '].children')) x
            WHERE m.Name = N'AllMenus' AND JSON_VALUE(x.value, '$.path') = N'/panel/crm/customercomplaint/list')
        UPDATE [system].[SystemMenu]
        SET Content = JSON_MODIFY(Content, CONCAT('append $[', @CrmOrd, '].children[', @OpOrd, '].children'), JSON_QUERY(@AllItem)),
            ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';

    DECLARE @ShowAllId BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'ShowAllMenus');
    IF @ShowAllId IS NOT NULL
    BEGIN
        DELETE FROM [system].[RoleAccess]
        WHERE RoleId = @ShowAllId AND Path LIKE N'/panel/crm/customercomplaint%';
        INSERT INTO [system].[RoleAccess]
            (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT g.Path, g.AType, g.IType, N'Entities.App.Crm.CustomerComplaint', @ShowAllId,
               1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM (VALUES
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/list', 1, 1),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/edit', 1, 5),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/new', 1, 4),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/save', 2, 3),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/add', 2, 4),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/update', 2, 5),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/delete', 2, 6),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/fetchdata', 2, 2),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/exporttoexcel', 2, 1000),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/sendreceiptsms', 2, 1000),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaint/sendclosedsms', 2, 1000),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/save', 2, 3),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/delete', 2, 6),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/edit', 1, 5),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/new', 1, 4),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintcontact/getlistbyparentid', 2, 2),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/save', 2, 3),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/delete', 2, 6),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/edit', 1, 5),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/new', 1, 4),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintaction/getlistbyparentid', 2, 2),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/save', 2, 3),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/delete', 2, 6),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/edit', 1, 5),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/new', 1, 4),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/listbyparentid', 1, 1),
        (N'Crm.Complaint.Manager', N'/panel/crm/customercomplaintattachment/getlistbyparentid', 2, 2)
        ) g(RoleName, Path, AType, IType);
        IF NOT EXISTS (
            SELECT 1 FROM [system].[RoleAccess]
            WHERE RoleId = @ShowAllId AND ActionAccessType = 3 AND RowId = @Id)
            INSERT INTO [system].[RoleAccess]
                (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES
                (N'dataProfile_' + CAST(@Id AS nvarchar(20)), 3, 7, N'Entities.App.Crm.CustomerComplaint', @Title, @Id, @ShowAllId,
                 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
    END

    COMMIT TRANSACTION;
    PRINT N'Crm_CustomerComplaint_List Id=' + CONVERT(nvarchar(20), @Id);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT q.Name, q.Title, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Crm_CustomerComplaint_List'
  AND JSON_VALUE(j.value, '$.Alliance') IN (N't1_SerialNumber', N't1_ComplaintStatus', N't2_FullName');

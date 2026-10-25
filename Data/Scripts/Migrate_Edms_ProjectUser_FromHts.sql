/*
  انتقال یک‌باره عضویت پروژه از HTS به Edms.ProjectUser
  و کپی کد سند، نام سند و نام کاربر روی Edms.ProjectAttachment.
  نمایه آرشیو بعد از این اسکریپت به HTS وصل نیست؛ این فایل فقط انتقال اولیه است.
  UTF-8 with BOM.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Migrate_Edms_ProjectUser_FromHts.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    INSERT INTO [Edms].[ProjectUser]
        (ProjectId, UserId, UserIsSupervisor, HtsId, IsActive, CreatedByName, CreatedOnMiladiDateTime)
    SELECT
        p.Id,
        u.Id,
        CAST(ISNULL(pu.UserIsSupervisor, 0) AS bit),
        CAST(pu.Edms_ProjectUser_ID AS bigint),
        1,
        N'migrate-edms-project-user',
        GETDATE()
    FROM [TMS].[TotalSystem].[dbo].[Edms_Project_User] AS pu
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu ON hu.User_ID = pu.User_FK
    INNER JOIN [Edms].[Project] AS p ON p.HtsId = pu.Project_FK AND p.HtsId <> 0
    CROSS APPLY (
        SELECT TOP (1) uu.Id
        FROM system.[User] AS uu
        WHERE NULLIF(LTRIM(RTRIM(uu.Username)), N'') IS NOT NULL
          AND (
                LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(ISNULL(hu.ActiveDirectoryUsername, N''))))
             OR LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(ISNULL(hu.Username, N''))))
          )
        ORDER BY CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(hu.ActiveDirectoryUsername, N''))), N'') IS NOT NULL
             AND LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(hu.ActiveDirectoryUsername))) THEN 0
            ELSE 1
        END, uu.Id
    ) AS u
    WHERE NOT EXISTS (
        SELECT 1
        FROM [Edms].[ProjectUser] AS t
        WHERE t.HtsId = pu.Edms_ProjectUser_ID
    );

    UPDATE pa
    SET DocumentCode = COALESCE(NULLIF(LTRIM(RTRIM(pa.DocumentCode)), N''), LEFT(hts.DocumentCode, 500)),
        DocumentName = COALESCE(NULLIF(LTRIM(RTRIM(pa.DocumentName)), N''), LEFT(hts.DocumentName, 500)),
        CreatedByName = COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedByName)), N''), LEFT(hu.FullName, 150)),
        CreatedById = COALESCE(pa.CreatedById, u.Id)
    FROM [Edms].[ProjectAttachment] AS pa
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Attachment] AS hts
        ON hts.Project_Attachment_ID = pa.HtsId AND pa.HtsId <> 0
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu ON hu.User_ID = hts.CreatedUser_FK
    OUTER APPLY (
        SELECT TOP (1) uu.Id
        FROM system.[User] AS uu
        WHERE hu.User_ID IS NOT NULL
          AND NULLIF(LTRIM(RTRIM(uu.Username)), N'') IS NOT NULL
          AND (
                LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(ISNULL(hu.ActiveDirectoryUsername, N''))))
             OR LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(ISNULL(hu.Username, N''))))
          )
        ORDER BY CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(hu.ActiveDirectoryUsername, N''))), N'') IS NOT NULL
             AND LOWER(LTRIM(RTRIM(uu.Username))) = LOWER(LTRIM(RTRIM(hu.ActiveDirectoryUsername))) THEN 0
            ELSE 1
        END, uu.Id
    ) AS u
    WHERE NULLIF(LTRIM(RTRIM(pa.DocumentCode)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(pa.DocumentName)), N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(pa.CreatedByName)), N'') IS NULL
       OR pa.CreatedById IS NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

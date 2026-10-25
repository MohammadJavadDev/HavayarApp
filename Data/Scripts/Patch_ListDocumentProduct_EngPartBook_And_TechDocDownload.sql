/*
  Patch_ListDocumentProduct_EngPartBook_And_TechDocDownload.sql
  1) Grant مشاهده دفترچه قطعات (/panel/inv/partbook/print) to engineering org units.
  2) Make پیوست مدارک فنی rows downloadable on ListDocumentProduct profiles.
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-ldp-eng-partbook';
    DECLARE @RoleId BIGINT = 600088;
    DECLARE @RoleName NVARCHAR(200) = N'Eng.DocumentProduct.PartBookView';
    DECLARE @RoleTitle NVARCHAR(200) = N'مهندسی - مشاهده دفترچه قطعات';
    DECLARE @PrintPath NVARCHAR(300) = N'/panel/inv/partbook/print';
    DECLARE @Script NVARCHAR(MAX) = N'async function(ctx) {
    if (!ctx.selectedRow) {
        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');
        return;
    }
    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);
    if (!response.isSuccess)
        return toastr.error(response.message);
    var rowsHtml = (response.data || []).map(function (x) {
        var fileId = x.attachmentId || x.AttachmentId;
        var downloadCell = fileId
            ? ''<button type="button" class="btn btn-sm btn-light-primary ldp-doc-download" data-file-id="'' + fileId + ''">دانلود</button>''
            : '''';
        return ''<tr>''
            + ''<td>'' + (x.originalName || '''') + ''</td>''
            + ''<td>'' + (x.size || '''') + ''</td>''
            + ''<td>'' + (x.type || '''') + ''</td>''
            + ''<td>'' + (x.main || '''') + ''</td>''
            + ''<td>'' + (x.createdByName || '''') + ''</td>''
            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''
            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''
            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''
            + ''<td class="text-center">'' + downloadCell + ''</td>''
            + ''</tr>'';
    }).join('''');
    var $modalContetn = $(''<table class="table table-bordered w-100">''
        + ''<thead><tr>''
        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''
        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th><th>دانلود</th>''
        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');
    $.confirm({
        title: ''پیوست مدارک'',
        content: $modalContetn,
        type: ''green'',
        columnClass: ''col-md-12'',
        typeAnimated: true,
        rtl: true,
        onContentReady: function () {
            this.$content.on(''click'', ''.ldp-doc-download'', function () {
                var fileId = $(this).data(''file-id'');
                if (!fileId) {
                    toastr.error(''فایل یافت نشد'');
                    return;
                }
                window.open(''/File/download/'' + fileId, ''_blank'');
            });
        },
        buttons: {
            close: {
                text: ''بستن'',
                btnClass: ''btn btn-danger'',
                action: function () {}
            }
        }
    });
}';

    IF @RoleName IS NULL OR @Script NOT LIKE N'%ldp-doc-download%' OR @Script NOT LIKE N'%دانلود%'
        THROW 51230, N'Script missing download action', 1;

    PRINT N'=== [1] Engineering part-book view role ===';
    IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @RoleId AND Name <> @RoleName)
        THROW 51231, N'Role id 600088 is already used by another name', 1;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@RoleId, @RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED ' + @RoleName;
    END
    ELSE
    BEGIN
        SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;
        UPDATE system.Role
        SET Title = @RoleTitle, IsActive = 1,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @RoleId;
        PRINT N'  EXISTS ' + @RoleName + N' Id=' + CAST(@RoleId AS nvarchar(20));
    END

    IF NOT EXISTS (
        SELECT 1 FROM system.RoleAccess
        WHERE RoleId = @RoleId AND Path = @PrintPath AND ActionAccessType = 1
    )
    BEGIN
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PrintPath, 1, 0, N'Entities.App.Inv.PartPartBookSection', N'مشاهده دفترچه قطعات', NULL, NULL, @RoleId,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        PRINT N'  RoleAccess inserted';
    END
    ELSE PRINT N'  RoleAccess exists';

    PRINT N'=== [2] Assign role to engineering org units ===';
    DECLARE @Assigned INT = 0, @Skipped INT = 0;
    DECLARE @UserId BIGINT, @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);

    DECLARE uc CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id
        FROM system.[User] u
        WHERE u.IsActive = 1
          AND u.OrgUnitId IN (1, 73, 114, 131, 132, 133);

    OPEN uc;
    FETCH NEXT FROM uc INTO @UserId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @RolesJson = Roles, @RoleIdsJson = RoleIds
        FROM system.[User] WHERE Id = @UserId;

        IF @RolesJson IS NULL OR LTRIM(RTRIM(@RolesJson)) = N'' SET @RolesJson = N'[]';
        IF @RoleIdsJson IS NULL OR LTRIM(RTRIM(@RoleIdsJson)) = N'' SET @RoleIdsJson = N'[]';

        IF ISJSON(@RolesJson) <> 1 OR ISJSON(@RoleIdsJson) <> 1
            SET @Skipped = @Skipped + 1;
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE [value] = @RoleName)
                SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleName);
            IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @RoleId)
                SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @RoleId);

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

        FETCH NEXT FROM uc INTO @UserId;
    END
    CLOSE uc;
    DEALLOCATE uc;
    PRINT N'  Users updated: ' + CAST(@Assigned AS nvarchar(20)) + N', skipped invalid JSON: ' + CAST(@Skipped AS nvarchar(20));

    PRINT N'=== [3] Download button on technical documents ===';
    DECLARE @Name NVARCHAR(200), @Json NVARCHAR(MAX), @Idx INT, @Path NVARCHAR(40), @Updated INT = 0;
    DECLARE c CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name FROM system.SavedQuery
        WHERE Name IN (
            N'ListDocumentProduct_NoPrice',
            N'vw_ListDocumentProductPricenew',
            N'vw_ListDocumentProductPriceSale',
            N'vw_ListDocumentProductPrice',
            N'ListDocumentProduct_Bom_NoPrice',
            N'ListDocumentProduct_Bom_Buy',
            N'ListDocumentProduct_Bom_Sale',
            N'vw_ListDocumentProductPriceBom'
        );

    OPEN c;
    FETCH NEXT FROM c INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @Json = CustomActionButtonsJson FROM system.SavedQuery WHERE Name = @Name;
        IF @Json IS NULL OR ISJSON(@Json) <> 1
            THROW 51232, N'CustomActionButtonsJson invalid', 1;

        SELECT @Idx = TRY_CAST([key] AS INT)
        FROM OPENJSON(@Json)
        WHERE JSON_VALUE([value], '$.id') = N'id_ldp_techdocs';

        IF @Idx IS NULL
            THROW 51233, N'techdocs button missing', 1;

        SET @Path = N'$[' + CAST(@Idx AS nvarchar(10)) + N'].actionScript';
        SET @Json = JSON_MODIFY(@Json, @Path, @Script);
        IF @Json IS NULL OR ISJSON(@Json) <> 1 OR @Json NOT LIKE N'%ldp-doc-download%' OR @Json NOT LIKE N'%دانلود%'
            THROW 51234, N'Patched button JSON invalid', 1;

        UPDATE system.SavedQuery
        SET CustomActionButtonsJson = @Json,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @Name;
        SET @Updated = @Updated + 1;
        SET @Idx = NULL;
        FETCH NEXT FROM c INTO @Name;
    END
    CLOSE c;
    DEALLOCATE c;
    PRINT N'  Profiles updated: ' + CAST(@Updated AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Patch_ListDocumentProduct_EngPartBook_And_TechDocDownload ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT r.Id, r.Name, r.Title,
       CASE WHEN r.Title LIKE N'%Ø%' OR r.Title LIKE N'%Ù%' THEN N'MOJIBAKE' ELSE N'OK' END AS TitleCheck
FROM system.Role r
WHERE r.Name = N'Eng.DocumentProduct.PartBookView';

SELECT COUNT(*) AS PrintGrants
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE r.Name = N'Eng.DocumentProduct.PartBookView'
  AND ra.Path = N'/panel/inv/partbook/print';

SELECT COUNT(DISTINCT u.Id) AS EngineeringUsers
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS BIGINT)
WHERE u.IsActive = 1
  AND r.Name = N'Eng.DocumentProduct.PartBookView';

SELECT q.Name,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%ldp-doc-download%' THEN 1 ELSE 0 END AS HasDownload,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%دانلود%' THEN 1 ELSE 0 END AS HasPersian,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%Ø%' OR q.CustomActionButtonsJson LIKE N'%Ù%' THEN N'MOJIBAKE' ELSE N'OK' END AS Enc
FROM system.SavedQuery q
WHERE q.Name IN (
    N'ListDocumentProduct_NoPrice',
    N'vw_ListDocumentProductPricenew',
    N'vw_ListDocumentProductPriceSale',
    N'vw_ListDocumentProductPrice',
    N'ListDocumentProduct_Bom_NoPrice',
    N'ListDocumentProduct_Bom_Buy',
    N'ListDocumentProduct_Bom_Sale',
    N'vw_ListDocumentProductPriceBom'
);

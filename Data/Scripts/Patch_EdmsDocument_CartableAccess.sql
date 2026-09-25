SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-cartable-access';
    DECLARE @Pid BIGINT;
    DECLARE @Title NVARCHAR(200);
    
    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_ProducerCartable';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS', N'کارشناسان بارگذاری اسناد EDMS', N'کارشناسان (خارجی) بارگذاری اسناد EDMS', N'کارشناسان بارگذاری/چک/تایید اسناد EDMS', N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی');

    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_ReviewApproveCartable';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS', N'EdmsDocumentsDccUsers', N'کارشناسان بارگذاری/چک/تایید اسناد EDMS', N'کارشناسان چک/تایید اسناد EDMS (خروجی اکسل)', N'کارشناسان مدیریت VPIS - پروپوزال (دسترسی مهندسی محصول)');

    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_DccApproveCartable';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS');

    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_ReadyTransmittal';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS');

    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_ClientReceive';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS');

    SELECT @Pid = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_AllRevisions';
    SELECT @Title = Title FROM system.SavedQuery WHERE Id = @Pid;
    DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, N'Entities.App.Edms.Document', @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers', N'Edms.Document.ChangeStatus', N'مدیر سیستم کنترل مستندات', N'مشاهده کلی HTS', N'مدیران پروژه در EDMS', N'کارشناسان آرشیو کلی اسناد EDMS', N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی', N'کارشناسان مدارک تکوین در EDMS', N'کارشناسان مشاهده کلی در آرشیو کلی اسناد EDMS');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT q.Title, r.Name AS RoleName
FROM system.SavedQuery q
JOIN system.RoleAccess ra ON ra.RowId = q.Id AND ra.ActionAccessType = 3
JOIN system.Role r ON r.Id = ra.RoleId
WHERE q.Name LIKE N'Edms_Document_%'
ORDER BY q.Title, r.Name;

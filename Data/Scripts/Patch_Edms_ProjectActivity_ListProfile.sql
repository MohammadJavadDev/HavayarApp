/*
Patch_Edms_ProjectActivity_ListProfile.sql
Data Profile `projectactivity_listinfo` (system.SavedQuery) for Edms.ProjectActivity.

After migrating HTS activities (Edms.ProjectActivity.ProjectId is now nullable and 20,774 rows have only
the free-text ProjectName), the grid must show:
  - ProjectName (free text)   : نام پروژه (متن)
  - Type                       : نوع فعالیت   (empty when HTS had no type)
and the default sort moves to تاریخ ایجاد descending (migrated rows have higher Ids than rows
entered in Portal, so sorting on Id would not show the newest first).

Idempotent: does nothing when t1_ProjectName is already in the query.
UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i ...
Verify after apply:
  SELECT JSON_VALUE(j.value,'$.Alliance'), JSON_VALUE(j.value,'$.DisplayName')
  FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j WHERE q.Name = N'projectactivity_listinfo';
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Name NVARCHAR(200) = N'projectactivity_listinfo';
    DECLARE @Id BIGINT, @Q NVARCHAR(MAX), @C NVARCHAR(MAX);
    SELECT @Id = Id, @Q = QueryJson, @C = ColumnsJson FROM system.SavedQuery WHERE Name = @Name;

    IF @Id IS NULL
        THROW 51731, N'SavedQuery projectactivity_listinfo not found.', 1;

    IF CHARINDEX(N't1_ProjectName', @Q) > 0
    BEGIN
        PRINT N'Already patched - nothing to do.';
        COMMIT TRANSACTION;
        RETURN;
    END;

    /* ---- QueryJson.CustomQuery: add ProjectName and Type after Id (JSON text: \n is a backslash + n) ---- */
    DECLARE @IdSel NVARCHAR(100) = N'[t1].[Id] AS [t1_Id],';
    IF CHARINDEX(@IdSel, @Q) = 0
        THROW 51732, N'Unexpected CustomQuery shape (t1_Id select not found).', 1;

    SET @Q = REPLACE(@Q, @IdSel,
        @IdSel + N'\n    [t1].[ProjectName] AS [t1_ProjectName],\n    [t1].[Type] AS [t1_Type],');

    /* ---- ColumnsJson: ProjectName and Type objects before the Comment column ---- */
    DECLARE @ProjCol NVARCHAR(MAX) = N'{"TableName":"Edms.ProjectActivity","ColumnName":"ProjectName","DisplayName":"'
        + N'نام پروژه (متن)'
        + N'","Alliance":"t1_ProjectName","Address":"[t1].[ProjectName]","SystemTypeName":"String","Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null},';
    DECLARE @TypeCol NVARCHAR(MAX) = N'{"TableName":"Edms.ProjectActivity","ColumnName":"Type","DisplayName":"'
        + N'نوع فعالیت'
        + N'","Alliance":"t1_Type","Address":"[t1].[Type]","SystemTypeName":null,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.ProjectActivityTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }"},';

    DECLARE @CommentMark NVARCHAR(100) = N'{"TableName":"Edms.ProjectActivity","ColumnName":"Comment"';
    IF CHARINDEX(@CommentMark, @C) = 0
        THROW 51733, N'Unexpected ColumnsJson shape (Comment column not found).', 1;

    SET @C = REPLACE(@C, @CommentMark, @ProjCol + @TypeCol + @CommentMark);

    /* ---- default sort: PK -> NULL, تاریخ ایجاد -> descending, SortOrder 0 ---- */
    SET @C = REPLACE(@C, N'"SystemType":6,"SortDirection":1,"SortOrder":1', N'"SystemType":6,"SortDirection":null,"SortOrder":null');
    SET @C = REPLACE(@C,
        N'"Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null',
        N'"Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0');

    IF ISJSON(@Q) <> 1 OR ISJSON(@C) <> 1
        THROW 51734, N'Patched JSON is not valid.', 1;

    UPDATE system.SavedQuery
    SET QueryJson = @Q,
        ColumnsJson = @C,
        ModifiedByName = N'patch-edms-projectactivity-profile',
        ModifiedDateMiladiDateTime = SYSDATETIME()
    WHERE Id = @Id;

    COMMIT TRANSACTION;
    PRINT N'Patched projectactivity_listinfo.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Phase 10 AgencyPartCardex + AgencyCartable
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase10';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;
BEGIN TRY
BEGIN TRANSACTION;
SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, MIN(nu.Id) AS NewUserId
INTO #UserMap FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu ON nu.Username = ou.Username GROUP BY CAST(ou.User_ID AS BIGINT);

MERGE Sale.AgencyPartCardex AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, c.Id AS AgencyId, p.Id AS PartId, s.Amount, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_AgencyPartCardex] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.AgencyId
    LEFT JOIN Inv.Part p ON p.HtsId = s.PartId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.AgencyId=s.AgencyId, t.PartId=s.PartId, t.Amount=s.Amount, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, AgencyId, PartId, Amount, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.AgencyId, s.PartId, s.Amount, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

MERGE Sale.AgencyCartable AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, um.NewUserId AS ResponsibleId, ums.NewUserId AS SupportResponsibleId,
           s.RequestDate AS RequestMiladiDate, s.RequestDateInText AS RequestShamsiDate, c.Id AS CustomerId, s.CustomerTitle,
           z.Id AS ZoneId, s.RequestComment, s.HasGuarantee, s.IsDone, sr.Id AS ServiceRequestId
    FROM [TMS].[TotalSystem].[dbo].[Sale_AgencyCartable] s
    LEFT JOIN #UserMap um ON um.OldUserId = s.ResponsibleId
    LEFT JOIN #UserMap ums ON ums.OldUserId = s.SupportResponsibleId
    LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerId
    LEFT JOIN Crm.Zone z ON z.HtsId = s.ZoneId
    LEFT JOIN Sale.ServiceRequest sr ON sr.HtsId = s.ServiceRequestId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ResponsibleId=s.ResponsibleId, t.SupportResponsibleId=s.SupportResponsibleId, t.RequestMiladiDate=s.RequestMiladiDate,
    t.RequestShamsiDate=s.RequestShamsiDate, t.CustomerId=s.CustomerId, t.CustomerTitle=s.CustomerTitle, t.ZoneId=s.ZoneId, t.RequestComment=s.RequestComment,
    t.HasGuarantee=s.HasGuarantee, t.IsDone=s.IsDone, t.ServiceRequestId=s.ServiceRequestId,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ResponsibleId, SupportResponsibleId, RequestMiladiDate, RequestShamsiDate, CustomerId, CustomerTitle, ZoneId, RequestComment, HasGuarantee, IsDone, ServiceRequestId,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ResponsibleId, s.SupportResponsibleId, s.RequestMiladiDate, s.RequestShamsiDate, s.CustomerId, s.CustomerTitle, s.ZoneId, s.RequestComment, s.HasGuarantee, s.IsDone, s.ServiceRequestId,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
COMMIT; PRINT N'Phase 10 sync committed';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;

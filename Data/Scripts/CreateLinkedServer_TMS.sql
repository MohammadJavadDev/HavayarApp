-- ============================================
-- ایجاد / بازسازی Linked Server: TMS
-- مقصد: TotalSystem روی 172.20.40.27
-- Provider: SQLNCLI11 (بدون خطای SSL مربوط به MSOLEDBSQL19)
-- Login: sa2 / Server@dmin_#
--
-- مهم: در SSMS روی Linked Server راست‌کلیک نکنید و
--      Provider را به Microsoft OLE DB Driver 19 تغییر ندهید.
--      برای تست فقط همین کوئری را اجرا کنید:
--      SELECT TOP 1 * FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest];
-- ============================================

SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    BEGIN TRY EXEC sp_droplinkedsrvlogin @rmtsrvname = N'TMS', @locallogin = NULL; END TRY BEGIN CATCH END CATCH;
    BEGIN TRY EXEC sp_droplinkedsrvlogin @rmtsrvname = N'TMS', @locallogin = N'sa'; END TRY BEGIN CATCH END CATCH;
    EXEC sp_dropserver @server = N'TMS', @droplogins = N'droplogins';
    PRINT N'Dropped old TMS';
END
GO

EXEC sp_addlinkedserver
    @server     = N'TMS',
    @srvproduct = N'',
    @provider   = N'SQLNCLI11',
    @datasrc    = N'172.20.40.27';
PRINT N'Created TMS with SQLNCLI11';
GO

EXEC sp_serveroption @server = N'TMS', @optname = N'data access', @optvalue = N'true';
EXEC sp_serveroption @server = N'TMS', @optname = N'rpc', @optvalue = N'true';
EXEC sp_serveroption @server = N'TMS', @optname = N'rpc out', @optvalue = N'true';
EXEC sp_serveroption @server = N'TMS', @optname = N'collation compatible', @optvalue = N'false';
EXEC sp_serveroption @server = N'TMS', @optname = N'use remote collation', @optvalue = N'true';
EXEC sp_serveroption @server = N'TMS', @optname = N'connect timeout', @optvalue = N'30';
EXEC sp_serveroption @server = N'TMS', @optname = N'query timeout', @optvalue = N'0';
EXEC sp_serveroption @server = N'TMS', @optname = N'lazy schema validation', @optvalue = N'false';
EXEC sp_serveroption @server = N'TMS', @optname = N'remote proc transaction promotion', @optvalue = N'false';
GO

-- حذف مپینگ پیش‌فرض (Use Self) که باعث Login failed for user sa می‌شود
EXEC sp_droplinkedsrvlogin @rmtsrvname = N'TMS', @locallogin = NULL;
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'TMS',
    @useself     = N'False',
    @locallogin  = NULL,
    @rmtuser     = N'sa2',
    @rmtpassword = N'Server@dmin_#';
GO

EXEC sp_addlinkedsrvlogin
    @rmtsrvname  = N'TMS',
    @useself     = N'False',
    @locallogin  = N'sa',
    @rmtuser     = N'sa2',
    @rmtpassword = N'Server@dmin_#';
GO

SELECT
    s.name AS LinkedServer,
    s.provider,
    s.data_source,
    ISNULL(p.name, N'(all)') AS LocalLogin,
    ll.remote_name AS RemoteLogin,
    ll.uses_self_credential AS UseSelf
FROM sys.servers s
INNER JOIN sys.linked_logins ll ON s.server_id = ll.server_id
LEFT JOIN sys.server_principals p ON p.principal_id = ll.local_principal_id
WHERE s.name = N'TMS';

SELECT TOP (1) OpenOrderRequest_ID AS TestOk
FROM [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest];

PRINT N'TMS linked server is ready.';
GO

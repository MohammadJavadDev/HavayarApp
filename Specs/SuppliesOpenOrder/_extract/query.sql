SET NOCOUNT ON;
-- TotalSystem (172.20.40.27) — Supplies OpenOrder comparison extract 2026-09-15

PRINT N'=== SYSTEM 13 ===';
SELECT System_ID, System_Title, System_Name FROM Gnr_System WHERE System_ID = 13;

PRINT N'=== PAGES ===';
SELECT p.Page_ID, p.PageTitle, p.PageName, p.System_FK, p.Report_FK
FROM Gnr_Page p
WHERE p.Page_ID IN (65,70,71,72,73,74,76,77,83,104,414,545)
ORDER BY p.Page_ID;

PRINT N'=== PAGE ACTIONS ===';
SELECT pa.PageAction_ID, pa.Page_FK, p.PageTitle, per.Permission_ID, per.Permission_Title, pa.Url
FROM Gnr_PageAction pa
JOIN Gnr_Page p ON p.Page_ID = pa.Page_FK
JOIN Gnr_Permission per ON per.Permission_ID = pa.Permission_FK
WHERE pa.Page_FK IN (65,70,71,72,73,74,76,77,83,104,414,545)
ORDER BY pa.Page_FK, per.Permission_ID;

PRINT N'=== LOOKUPS ===';
SELECT glt.LookupType_ID, glt.LookupType_Title_Fa, gl.Lookup_ID, RTRIM(gl.Lookup_Title_Fa) AS Lookup_Title_Fa, gl.LookupCode
FROM Gnr_Lookup gl
JOIN Gnr_LookupType glt ON gl.LookupType_FK = glt.LookupType_ID
WHERE gl.LookupType_FK IN (66,162,346,347,348,350) OR gl.Lookup_ID IN (2829,2830,264,265,271,308,307)
ORDER BY glt.LookupType_ID, gl.Lookup_ID;

PRINT N'=== TRIGGERS ===';
SELECT t.name AS TriggerName, OBJECT_SCHEMA_NAME(t.parent_id) AS SchemaName, OBJECT_NAME(t.parent_id) AS TableName, t.is_disabled, t.is_instead_of_trigger
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) IN (
  'Sup_OpenOrderRequest','Sup_OpenOrderRequest_Comment','Sup_OpenOrderRequest_Attachment',
  'Sup_OpenOrderRequest_EmailToSupplier','Sup_OpenOrderRequest_Event','Sup_OpenOrderRequest_Requested_Personel',
  'Sup_OpenOrderRequestVpis','Sup_BuyCategory','Sup_BuyCategory_Part','Sup_BuyCategory_Company',
  'Pln_LeadTime','Inv_Part_Company','Gnr_ManCompany'
)
ORDER BY TableName, TriggerName;

PRINT N'=== PROCEDURES ===';
SELECT name AS ProcName FROM sys.procedures
WHERE name LIKE '%OpenOrder%' OR name LIKE '%BuyCategory%' OR name LIKE '%LeadTime%'
ORDER BY name;

PRINT N'=== ROW COUNTS ===';
SELECT
  (SELECT COUNT(*) FROM Sup_OpenOrderRequest WHERE IsDeleted = 0) AS ActiveOpen,
  (SELECT COUNT(*) FROM Sup_OpenOrderRequest WHERE IsDeleted = 1) AS DeletedOpen,
  (SELECT COUNT(*) FROM Sup_OpenOrderRequest_EmailToSupplier) AS EmailToSupplier,
  (SELECT COUNT(*) FROM Sup_BuyCategory) AS BuyCategory,
  (SELECT COUNT(*) FROM Sup_BuyCategory_Part) AS BuyCategoryPart,
  (SELECT COUNT(*) FROM Sup_BuyCategory_Company) AS BuyCategoryCompany,
  (SELECT COUNT(*) FROM Pln_LeadTime) AS LeadTime,
  (SELECT COUNT(*) FROM Inv_Part_Company) AS PartCompany,
  (SELECT COUNT(*) FROM Gnr_ManCompany WHERE ManCompany_Type = 'COMP') AS CompaniesComp;

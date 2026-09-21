SET NOCOUNT ON;

PRINT N'=== SYSTEMS ===';
SELECT System_ID, System_Title FROM Gnr_System WHERE System_ID IN (14, 5) ORDER BY System_ID;

PRINT N'=== REPORTS SYSTEM 14 ===';
SELECT r.Report_ID, r.Report_Name, r.Report_Title, r.Report_Order, r.Report_Parent_FK
FROM Gnr_Report r WHERE r.System_FK = 14
ORDER BY r.Report_Order, r.Report_ID;

PRINT N'=== PAGES SYSTEM 14 ===';
SELECT p.Page_ID, p.PageTitle, p.PageName, p.Report_FK
FROM Gnr_Page p
WHERE p.System_FK = 14
ORDER BY p.Page_ID;

PRINT N'=== SURVEY TYPE LOOKUPS ===';
SELECT gl.Lookup_ID, gl.Lookup_Title_Fa, gl.Lookup_Title_En, gl.LookupCode, gl.LookupType_FK, glt.LookupType_Title
FROM Gnr_Lookup gl
LEFT JOIN Gnr_LookupType glt ON gl.LookupType_FK = glt.LookupType_ID
WHERE gl.Lookup_ID IN (1007, 1008, 2256, 2332, 1691)
ORDER BY gl.Lookup_ID;

PRINT N'=== LOOKUP TYPES USED BY SALE ===';
SELECT glt.LookupType_ID, glt.LookupType_Title, gl.Lookup_ID, RTRIM(gl.Lookup_Title_Fa) AS Lookup_Title_Fa, gl.LookupCode
FROM Gnr_Lookup gl
JOIN Gnr_LookupType glt ON gl.LookupType_FK = glt.LookupType_ID
WHERE gl.LookupType_FK IN (9,30,40,42,55,56,62,67,69,71,75,88,94,95,96,132,142,146,174,175,200,201,213,221,246,292,303,305,306,333,340,341,364,368,369,371)
ORDER BY glt.LookupType_ID, gl.Lookup_ID;

PRINT N'=== REQUEST TYPES ===';
SELECT RequestType_ID, RequestType_Title FROM Sale_RequestType ORDER BY RequestType_ID;

PRINT N'=== ACTION TYPES ===';
SELECT * FROM Sale_ActionType;

PRINT N'=== REPAIRS TYPES ===';
SELECT * FROM Sale_RepairsType;

PRINT N'=== ZONE ===';
SELECT Zone_ID, Zone_Title, Zone_SupervisorUser_FK, ZoneType_FK FROM Crm_Zone ORDER BY Zone_ID;

PRINT N'=== ZONE TYPE ===';
SELECT * FROM Crm_ZoneType;

PRINT N'=== SURVEY TYPE DISTRIBUTION ===';
SELECT TypeId, COUNT(*) AS Cnt FROM Crm_CustomerSatisfactionSurvey GROUP BY TypeId ORDER BY TypeId;

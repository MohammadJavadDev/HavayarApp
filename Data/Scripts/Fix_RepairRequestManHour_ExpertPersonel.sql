/*
  RepairRequestManHour.ExpertId must point at Hcm.Personel, not system.User.
  HTS Personel_FK is HRM_Personel.Personel_ID; map Hamkaran_Personel_FK -> Hcm.Personel.Code.
  75 contractor rows have no Hamkaran code and stay NULL.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RepairRequestManHour_User_ExpertId')
        ALTER TABLE Rpr.RepairRequestManHour DROP CONSTRAINT FK_RepairRequestManHour_User_ExpertId;

    UPDATE mh
    SET mh.ExpertId = NULL
    FROM Rpr.RepairRequestManHour mh;

    UPDATE mh
    SET mh.ExpertId = pe.Id
    FROM Rpr.RepairRequestManHour mh
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hrm ON hrm.Personel_ID = mh.HtsPersonelId
    CROSS APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE ISNULL(hrm.Hamkaran_Personel_FK, 0) <> 0
          AND TRY_CONVERT(int, p.Code) = hrm.Hamkaran_Personel_FK
        ORDER BY p.Id
    ) pe;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RepairRequestManHour_Personel_ExpertId')
        ALTER TABLE Rpr.RepairRequestManHour
        ADD CONSTRAINT FK_RepairRequestManHour_Personel_ExpertId
        FOREIGN KEY (ExpertId) REFERENCES Hcm.Personel (Id);

    DECLARE @ExpertExpr NVARCHAR(300) = N'LTRIM(RTRIM(CONCAT(ISNULL([t3].[Code], N''''), N'' '', ISNULL([t3].[Name], N''''), N'' '', ISNULL([t3].[Family], N''''))))';

    UPDATE system.SavedQuery
    SET QueryJson = REPLACE(
            REPLACE(QueryJson,
                N'LEFT JOIN [system].[User] AS [t3] ON [t1].[ExpertId] = [t3].[Id]',
                N'LEFT JOIN [Hcm].[Personel] AS [t3] ON [t1].[ExpertId] = [t3].[Id]'),
            N'[t3].[Name] AS [t3_Name]',
            @ExpertExpr + N' AS [t3_Name]'),
        ColumnsJson = REPLACE(
            REPLACE(ColumnsJson,
                N'"TableName":"system.User","ColumnName":"Name"',
                N'"TableName":"Hcm.Personel","ColumnName":"Name"'),
            N'"Alliance":"t3_Name","Address":"[t3].[Name]"',
            N'"Alliance":"t3_Name","Address":"' + @ExpertExpr + N'"')
    WHERE Name = N'AfterSales_RepairRequestManHour_List';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    SUM(CASE WHEN mh.ExpertId IS NOT NULL THEN 1 ELSE 0 END) AS MappedToPersonel,
    SUM(CASE WHEN mh.ExpertId IS NULL THEN 1 ELSE 0 END) AS Unmapped,
    SUM(CASE WHEN u.Id IS NOT NULL AND p.Id IS NULL THEN 1 ELSE 0 END) AS StillUserOnly
FROM Rpr.RepairRequestManHour mh
LEFT JOIN [system].[User] u ON u.Id = mh.ExpertId
LEFT JOIN Hcm.Personel p ON p.Id = mh.ExpertId;

SELECT JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.TableName') AS TableName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_RepairRequestManHour_List'
  AND JSON_VALUE(j.value, '$.Alliance') = N't3_Name';

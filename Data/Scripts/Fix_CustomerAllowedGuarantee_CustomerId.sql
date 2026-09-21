/*
  Fix_CustomerAllowedGuarantee_CustomerId.sql
  Phase4 initially joined only SLS.Customer.HtsId = HTS Customer_FK.
  After Customer.HtsId widen, rematch; leftover rows use unique Customer.Code = Crm_Customer.Customer_Code.
  Does not invent customers or overwrite Customer.HtsId.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'fix-customerallowedguarantee-customerid';

    ;WITH UniqueCustomerCode AS (
        SELECT Code
        FROM SLS.Customer
        WHERE Code IS NOT NULL
        GROUP BY Code
        HAVING COUNT(*) = 1
    ),
    Map AS (
        SELECT
            g.Id,
            COALESCE(cHts.Id, cCode.Id) AS CustomerId
        FROM Sale.CustomerAllowedGuarantee g
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Customer_AllowedGuarantee] s
            ON s.Customer_AllowedGuarantee_ID = g.HtsId
        LEFT JOIN SLS.Customer cHts ON cHts.HtsId = s.Customer_FK AND cHts.HtsId <> 0
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] hc ON hc.Customer_ID = s.Customer_FK
        LEFT JOIN UniqueCustomerCode uc ON uc.Code = hc.Customer_Code
        LEFT JOIN SLS.Customer cCode ON cCode.Code = uc.Code
    )
    UPDATE g
    SET g.CustomerId = m.CustomerId,
        g.ModifiedById = 1,
        g.ModifiedByName = @SeedUser,
        g.ModifiedDateMiladiDateTime = @Now,
        g.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.CustomerAllowedGuarantee g
    INNER JOIN Map m ON m.Id = g.Id
    WHERE m.CustomerId IS NOT NULL
      AND (g.CustomerId IS NULL OR g.CustomerId <> m.CustomerId);
    PRINT N'  Remapped CustomerId rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_CustomerAllowedGuarantee_CustomerId ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS Unmapped,
    SUM(CASE WHEN CustomerId IS NOT NULL THEN 1 ELSE 0 END) AS Mapped
FROM Sale.CustomerAllowedGuarantee;

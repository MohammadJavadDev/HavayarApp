-- بازگرداندن تلفن مستقیم و شماره داخلی شرکت‌کننده از TotalSystem
-- فقط وقتی مقدار فعلی هنوز همان مقدار مهاجرت‌شده (برش ۲۰ نویسه) است بازنویسی می‌شود.
-- سمت‌هایی که فقط در JobPosition هستند و Workplace خالی است به Workplace کپی می‌شوند.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE p
    SET DirectPhoneNumber = NULLIF(LTRIM(RTRIM(o.DirectPhoneNumber)), N'')
    FROM Trn.Participant p
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Participant] o
        ON o.Id = p.Id
    WHERE p.DirectPhoneNumber IS NULL
      AND NULLIF(LTRIM(RTRIM(o.DirectPhoneNumber)), N'') IS NOT NULL;

    UPDATE p
    SET Phone = NULL
    FROM Trn.Participant p
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Participant] o
        ON o.Id = p.Id
    WHERE NULLIF(LTRIM(RTRIM(o.PhoneNumber)), N'') IS NULL
      AND NULLIF(LTRIM(RTRIM(o.DirectPhoneNumber)), N'') IS NOT NULL
      AND p.Phone = LEFT(LTRIM(RTRIM(o.DirectPhoneNumber)), 20);

    UPDATE p
    SET Phone = NULLIF(LTRIM(RTRIM(o.PhoneNumber)), N'')
    FROM Trn.Participant p
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Participant] o
        ON o.Id = p.Id
    WHERE NULLIF(LTRIM(RTRIM(o.PhoneNumber)), N'') IS NOT NULL
      AND LEN(LTRIM(RTRIM(o.PhoneNumber))) > 20
      AND p.Phone = LEFT(LTRIM(RTRIM(o.PhoneNumber)), 20);

    UPDATE p
    SET Mobile = NULLIF(LTRIM(RTRIM(o.MobileNumber)), N'')
    FROM Trn.Participant p
    INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Participant] o
        ON o.Id = p.Id
    WHERE NULLIF(LTRIM(RTRIM(o.MobileNumber)), N'') IS NOT NULL
      AND LEN(LTRIM(RTRIM(o.MobileNumber))) > 20
      AND p.Mobile = LEFT(LTRIM(RTRIM(o.MobileNumber)), 20);

    UPDATE Trn.Participant
    SET Workplace = JobPosition
    WHERE (Workplace IS NULL OR LTRIM(RTRIM(Workplace)) = N'')
      AND JobPosition IS NOT NULL
      AND LTRIM(RTRIM(JobPosition)) <> N'';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    (SELECT COUNT(*) FROM Trn.Participant WHERE DirectPhoneNumber IS NOT NULL AND DirectPhoneNumber <> N'') AS DirectCount,
    (SELECT COUNT(*) FROM Trn.Participant WHERE Phone IS NOT NULL AND Phone <> N'') AS InternalCount,
    (SELECT Phone FROM Trn.Participant WHERE Id = 666) AS Id666Phone,
    (SELECT DirectPhoneNumber FROM Trn.Participant WHERE Id = 666) AS Id666Direct;

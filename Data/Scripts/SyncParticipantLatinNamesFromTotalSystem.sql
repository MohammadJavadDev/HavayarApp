-- ============================================
-- انتقال نام لاتین شرکت‌کنندگان از TotalSystem
-- به Trn.Participant بدون پاک کردن ردیف‌های فعلی
--
-- منبع: dbo.Training_Participant.FirstNameInLatin / LastNameInLatin
-- تطبیق: Id
-- ============================================

SET NOCOUNT ON;

UPDATE p
SET
    p.FirstNameInLatin = NULLIF(LTRIM(RTRIM(tp.FirstNameInLatin)), N''),
    p.LastNameInLatin  = NULLIF(LTRIM(RTRIM(tp.LastNameInLatin)), N'')
FROM Trn.Participant AS p
INNER JOIN [TMS].[TotalSystem].[dbo].[Training_Participant] AS tp
    ON tp.Id = p.Id;

SELECT
    @@ROWCOUNT AS UpdatedRows,
    (SELECT COUNT(*) FROM Trn.Participant WHERE FirstNameInLatin IS NOT NULL) AS WithFirstNameInLatin,
    (SELECT COUNT(*) FROM Trn.Participant WHERE LastNameInLatin IS NOT NULL) AS WithLastNameInLatin,
    (SELECT COUNT(*) FROM Trn.Participant) AS ParticipantCount;

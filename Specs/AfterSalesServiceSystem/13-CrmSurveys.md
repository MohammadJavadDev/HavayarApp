# Part 13 — After-sales CRM surveys

**In scope TypeId** (counts from TotalSystem 2026-09-15):

| TypeId | Use | Count |
|---|---|---|
| 1008 | بعد از فروش (HTS after-sale controller) | 556 |
| 1942 | بعد از فروش CNG | 14 |
| 2332 | خدمات هوای فشرده | 124 |

**Out:** 1007 / 1691 / 2256 and all complaint / general CRM satisfaction.

HTS CNG after-sale UI filtered **1008**; new pages filter by TypeId on List (CompressedAirServices = 2332, CngAfterSales = 1942, default List = 1008).

## Entities

`Crm.CustomerSatisfactionSurvey` + `Result` + `Attachment`. Question bank stays HTS ids on results for the first sync (`QuestionHtsId`).

## Permission

`SendingSatisfactionLink` (HTS 174) = `CustomerSatisfactionSurveyController.SendingSatisfactionLink` (Custom). SMS send from work report uses this action; do not hide it inside generic Update.

Print Likert = Stimulsoft. On-screen KPI = Apex on dashboard filters, not Stimulsoft.

## Sync

`SyncAfterSalesPhase13FromTotalSystem.sql` — `WHERE TypeId IN (1008,1942,2332)`.

## Status (2026-09-15)

Survey List + three type-filtered Data Profiles (1008/1942/2332). `SendSatisfactionLink` writes an Email `Notification` with the HTS SMS/email body (Havayar has no SMS `NotificationType`; EmailJob sends the mail). Likert print wires `ViewReportByName/CustomerSatisfactionLikert` (template must exist in ReportBuilder). Public `/ServiceSatisfaction` capture page is not ported.

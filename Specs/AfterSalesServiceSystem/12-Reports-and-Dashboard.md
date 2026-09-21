# Part 12 — Reports and dashboard

TotalSystem catalog is `Gnr_Report` **`System_FK = 14`** (not `System_ID`). Extract: `_extract/reports.csv` (32 rows, queried 2026-09-15).

## Implementation law (`havayar-reports.mdc`)

| Need | Path |
|---|---|
| Tabular / Excel | Data Profile (`Type = DataProfile`) on the entity List page |
| KPI / bar / pie | ApexCharts page — `AfterSalesDashboardController` copies `CourseStatisticalReport` / `PlnTimelyDeliveryReport` **but GetData stays in the controller** (no new service) |
| Official print | ReportBuilder Stimulsoft `.mrt` (`ServiceRequestSheet`, `RepairRequestLabel`, `CollectionClaimPayFish`, Likert survey form) |

Do **not** use Stimulsoft chart bands for the on-screen dashboard. Do **not** add Chart.js/Highcharts.

## Pages

- `/panel/sale/aftersalesdashboard/report` — KPI + bar
- `/panel/rpr/repairrequest/delaychart` — repair delay chart
- `/panel/rpr/repairrequest/delaylist` — delay grid
- Menu «گزارشات» leaves point at List pages + Data Profiles named `AfterSales_{Gnr_Report.Report_Name}`

Fixed menu reports (استطاعت، مانده بدهی، جامع فروش، وصول) = Data Profiles on `OrderDetail` / `CollectionClaim` / `Customer` as soon as SavedQuery SQL is filled from the HTS view text.

## Seed

`Data/Scripts/Seed_AfterSales_ReportDataProfiles.sql` — stub profile names matching `Gnr_Report.Report_Name`; fill `CustomQuery` from HTS `ReadData` views when promoting a report to production.

## Status (2026-09-15)

`AfterSalesDashboard/Report` + `GetData` KPI counts (requests waiting, missions, repairs, delays, collections). Delay chart page exists. The six named System-14 SavedQueries (`AfterSales_Sale_Mission`, `_ServiceRequestPart`, `_Order`, `_CustomerDebit`, `_RepairsReport`, `_EstimateCost`) now run live Havayar SQL (not `WHERE 1=0`). Remaining HTS `Gnr_Report` rows that have no Havayar view equivalent stay unseeded.

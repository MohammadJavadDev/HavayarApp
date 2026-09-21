# Trn Reports — Course / Participant / Statistical Reports (گزارش‌ها)

> Prerequisite: read `00-Overview-and-Conventions.md` §7 and `.cursor/rules/havayar-reports.mdc`.
> Tabular = Data Profile. Charts = ApexCharts page. Print only = Stimulsoft/ReportBuilder.

## 1. Purpose

The three legacy report pages under Training System → Reports. Implementation path differs per report kind — **do not** turn all three into Stimulsoft templates.

Legacy source: `CourseReportController` + `_CourseReport.cshtml`, `ParticipantReportController` + `_ParticipantReport.cshtml`, statistical/total flow (`TrainingTotalReportModel` + `_StatisticalReport.cshtml` + `_StatisticalReportCharts.cshtml`, backed by 3 stored procedures + 5 charts). Menu: Training System → Reports → (CourseReport, ParticipantReport, StatisticalReport).

> **Not ported:** `_CourseTotalReport.cshtml` (orphaned legacy view, never called by any action — superseded by the statistical report below).

## 2. Report A — Course list report (گزارش دوره‌ها) — **Data Profile**

- **Legacy behavior:** filterable grid of courses (by date range, course field/bank, teacher, status, executing method) with export.
- **New implementation:** a Data Profile on the existing `Course` List page (`<datatableprofile entity-Type="typeof(Course)">`), per `havayar-dataprofile-savedquery.mdc`. Query over `Trn.Course` joined to `CourseBank`, `TeacherBank` (teacher + second teacher), `Company`, `Agent`, `Region` (location), `OrgUnit` (related unit). Exposed columns: `TrainingCode`, course-field title, `StartShamsiDate`/`EndShamsiDate`, `DurationInMinute`, teacher full names, company title, agent full name, location, `Capacity`/`RemainingCapacity`, all six status enum display names, `Cost`, participant counts (both tracks).
- Filters: Shamsi date range, course field, teacher, status, executing method — same filter set as the legacy `_CourseReport.cshtml` (Data Profile / DataTable filters, not ReportBuilder parameters).
- Launched from: Course List (profile dropdown) and/or Reports menu → "گزارش دوره‌ها" pointing at `/panel/trn/course/list`. Excel via the Data Profile toolbar.

## 3. Report B — Participant union report (گزارش شرکت‌کنندگان) — **Data Profile**

- **Legacy behavior:** a 3-branch UNION query merging public-track participants, private-track participants, and standalone participant master rows into one searchable grid (by participant name/national code, course, company, date range).
- **New implementation:** a Data Profile (on `Participant` List, or a dedicated List page with `<datatableprofile>` if the union cannot hang off `Participant`) replicating exactly that 3-branch union:
  1. `CourseParticipant` ⨝ `Participant` ⨝ `Course` (+ `CourseBank`) — with a literal `'عمومی'` track column,
  2. `CourseCompanyParticipant` ⨝ `Participant` ⨝ `Course` ⨝ `Company` — with a literal `'اختصاصی'` track column,
  3. `Participant` master rows left-joined to their course enrollments (to include people never enrolled in any course).
  Exposed columns: participant full name, father name, national code, gender display name, mobile, company title (branch 2/3), course training code + field title + dates (branches 1/2), track literal, register-status display name.
- Filters: participant name/national code, course, company, Shamsi date range.
- Launched from: Reports menu → "گزارش شرکت‌کنندگان". Excel via the Data Profile toolbar — **not** Stimulsoft.

## 4. Report C — Statistical / total report (گزارش آماری) — **ApexCharts page (already exists)**

- **Legacy behavior:** aggregate dashboard — counts/sums over courses/participants/certificates (the `TrainingTotalReportModel` shape) rendered as summary tables **plus 5 charts** (`_StatisticalReportCharts.cshtml`), computed by 3 stored procedures.
- **New implementation:** dedicated page using the existing ApexCharts stack — **already shipped** as:
  - `WebApp/Controllers/Dynamic/Trn/CourseStatisticalReportController.cs` (`Report` + `GetData`)
  - `WebApp/Views/Panel/Trn/CourseStatisticalReport/Report.cshtml`
- Do **not** recreate these charts as Stimulsoft `.mrt` chart bands.
- Parameters: Shamsi date range (+ optional course-field filter).
- Launched from: Reports menu → "گزارش آماری" → `/panel/trn/coursestatisticalreport/report`.
- If a **printable** copy of the same dashboard is later requested, that print layout can be a Stimulsoft template **in addition** to this page.

## 5. Wiring checklist (for whoever implements this file)

1. Report A/B: seed Data Profiles (`Type = DataProfile`) + `RoleAccess` per `havayar-dataprofile-savedquery.mdc` (names suggestion: `Trn_CourseReport`, `Trn_ParticipantReport`).
2. Report C: already an ApexCharts page — only confirm the menu path; do not add a Stimulsoft template unless print is explicitly requested.
3. Add Reports-menu entries: A/B → the List page that hosts the profile; C → `CourseStatisticalReport/Report`.
4. Certificates and other **print** layouts stay Stimulsoft — see `09-Certificate-and-CertificateEmailLog.md`.

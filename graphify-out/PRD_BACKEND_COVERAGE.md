# Backend PRD v2 Coverage - Helpdesk

Date: 2026-05-07

Scope: backend only. Frontend coverage is intentionally not evaluated here.

## Status Legend
- Done: backend behavior appears materially implemented.
- Partial: model/endpoints/services exist, but requirements are incomplete or risky.
- Missing: no meaningful backend implementation found.

## Requirement Coverage

| Section | Requirement Area | Status | Backend Evidence | Remaining Work |
|---|---|---:|---|---|
| 2 | Roles and permissions | Partial | JWT auth, role guards, `CurrentUserService`, service-level filters | Normalize `Agent`/`SupportAgent`; enforce Department Head view-only boundaries consistently. |
| 3 | Ticket fields | Improved | `Ticket` has asset, related ticket, SLA, escalation fields; Department is now required; response includes SLA metadata | Ensure all clients pass department or rely on backend fallback; apply migration. |
| 4 | Ticket lifecycle | Improved | `TicketStatus`, update/reopen/escalate methods; delete archives; archived tickets read-only | Confirm archive filters in all future endpoints. |
| 5 | Email notifications | Improved | `NotificationOutbox`, `NotificationService`, `EmailBackgroundWorker`, templates; DB SMTP settings and plain-text fallback now used | Complete trigger matrix; add provider switch; log all send attempts as audit/structured records. |
| 6 | SLA | Improved | `SlaCalculationEngine`, `SlaMonitorWorker`, pause fields; configurable resolution targets; first response and close compliance fields | Add dedicated first-response target tracking and richer SLA reporting. |
| 7 | Reporting | Partial | `ReportsController`, `ReportingService`, async report queue; agents are scoped to own data | Implement true reports, trends, charts, full filters, async email/download flow. |
| 8 | Audit trail | Improved | `AuditLogService`, append-only EF behavior, auth success/failure/logout logging, filtered audit search/export | Add full previous/new value detail capture across all update services. |
| 9 | Knowledge Base | Improved | `KBArticleService`, versions, search, suggestions, feedback, submit-review/approve/reject workflow | Add attach KB to comments; ticket-form suggestion solve/dismiss flow. |
| 10 | Escalation workflow | Partial | `EscalateTicketAsync`, `SlaMonitorWorker` auto-escalations | Fix user reopen path; bump priority for auto escalation; sort escalated tickets first; preserve flag while clearing highlight on acknowledgement. |
| 11 | CSAT | Partial | `SurveyService`, duplicate prevention, expiry check | Add delayed survey email; visible ticket survey link; strict aggregate-only agent access; report/dashboard calculations. |
| 12 | Recurring templates | Improved | `RecurringTemplateService`, `RecurringTicketWorker`, run logs, manual trigger, occurrence limits, validation, generated-ticket audit logging | Add friendlier daily/weekly/monthly schedule abstraction if needed by clients. |
| 13 | Departments | Improved | `DepartmentService`, `Department` model, summaries; unique name and deactivation guards added | Implement user move audit behavior. |
| 14 | Admin settings | Partial | `SystemSettingService`, SMTP endpoints, logo upload, archival setting | Enforce session timeout; notification event toggles; provider credentials; user import validation; DB SMTP used by worker. |
| 14.6 | User import | Improved | `UserService.ImportUsersFromCsvAsync` now expects `Name, Email, Department, Role` and validates all rows before creating | Return a richer import summary DTO instead of count-only. |
| 14.7 | Data retention | Improved | `TicketArchivalWorker`, `Archived` status; manual delete path archives instead of hard-deleting | Ensure future ticket endpoints preserve read-only archive semantics. |

## Backend Build Verification

Passed:

```powershell
dotnet build C:\Heldesk_fullstack\Helpdesk\Helpdesk.sln /p:UseAppHost=false
```

Warnings to track:
- `AutoMapper 12.0.1` high-severity advisory.
- Locked `Helpdesk.exe/apphost.exe` prevents normal apphost build cleanup in the current environment.

## Priority Backlog

1. Exact PRD reports, PDF chart output, and richer report summaries.
2. Complete notification matrix and optional provider switch.
3. KB ticket-comment attach flow and ticket-form solve/dismiss flow.
4. Full previous/new value capture for audit details across all update services.
5. Dependency and warning cleanup.

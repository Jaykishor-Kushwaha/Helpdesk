# Graph Report - Helpdesk Backend (2026-05-12)

## Scope
- Backend only: `C:\Heldesk_fullstack\Helpdesk\Helpdesk`
- Frontend was intentionally excluded from this update.
- PRD source: `HelpDesk_PRD_v2.docx`

## Build Check
- `dotnet build` passed after full PRD v2 gap integration:
  `0 Error(s) | 55 Warning(s) (all nullable reference, no actionable issues)`
- Warning to track: `AutoMapper 12.0.1` has a high-severity advisory.
- Migration applied: `PRD_v2_GapCompletion2` — schema updated successfully.

---

## Summary
- All four PRD v2 gaps are now integrated into the backend.
- Estimated backend PRD v2 coverage: **~97%**
- Remaining gap: Angular frontend KB suggest wiring (ticket-form debounced suggest + solve/dismiss).

---

## What Changed Since Last Graph (2026-05-07)

### New Files Added
| File | Purpose |
|---|---|
| `Services/NotificationService.cs` | Full §5.2 trigger matrix with preference checks |
| `Services/EmailTemplateService.cs` | Typed HTML/plaintext template rendering |
| `Services/ReportingService.partial.cs` | All 9 PRD report types (TicketVolume, SLACompliance, Ageing, etc.) |
| `Services/KBArticleService.partial.cs` | `SuggestAsync`, `RecordSolvedAsync`, `AttachToCommentAsync` |
| `Services/AuditLogService.partial.cs` | Typed audit methods with old/new value capture |
| `Controllers/KBSuggestController.cs` | `POST /api/kb/suggest` + `POST /api/kb/suggest/record-solved` |
| `Models/KBSolveEvent.cs` | Tracks KB article solve/dismiss events |
| `Models/KBCommentAttachment.cs` | Tracks KB articles attached to comments |

### Modified Files
| File | Change |
|---|---|
| `Interfaces/ICurrentUserService.cs` | Added `FullName`, `Email`, `IpAddress` |
| `Services/CurrentUserService.cs` | Implemented `FullName`, `Email`, `IpAddress` from claims/HttpContext |
| `Interfaces/INotificationService.cs` | Full trigger matrix method contracts |
| `Interfaces/IAuditLogService.cs` | Typed audit methods (LogTicketCreatedAsync, LogSlaOverriddenAsync, etc.) |
| `Interfaces/IKBArticleService.cs` | `RecordSolvedAsync`, `AttachToCommentAsync` |
| `Services/TicketService.cs` | All 7 operations wired to typed audit + notification methods |
| `Services/CommentService.cs` | `SendNewCommentAsync` replaces manual QueueEmailAsync in AddCommentAsync |
| `Services/KBArticleService.cs` | All 6 KB audit calls replaced with typed equivalents |
| `Services/ReportingService.cs` | Converted to `partial`, routes 9 report types |
| `Services/AuditLogService.cs` | Converted to `partial` |
| `Services/KBArticleService.cs` | Converted to `partial` |
| `Data/AppDbContext.cs` | Added `KBSolveEvent`/`KBCommentAttachment` DbSets + Fluent API |
| `Models/Ticket.cs` | Added `IsAutoEscalated`, `ResolvedAt`, `ResolutionSummary` |
| `Program.cs` | Registered `IEmailTemplateService → EmailTemplateService` |
| `Controllers/KBArticlesController.cs` | Made `partial` |
| `Controllers/TicketsController.cs` | Made `partial` |

### Database Migrations
| Migration | Applied |
|---|---|
| `PRD_v2_GapCompletion2` | ✅ Yes — added `IsAutoEscalated`, `ResolvedAt`, `ResolutionSummary` columns to `Tickets`; created `KBSolveEvents` table with composite index |

---

## Community Hubs (Updated)

| Hub | Key Nodes |
|---|---|
| Ticket Lifecycle | `TicketsController`, `TicketService` (partial), `Ticket`, `CommentService` |
| SLA & Escalation | `SlaCalculationEngine`, `SlaMonitorWorker`, escalation methods in `TicketService` |
| Notifications | `NotificationService` (full trigger matrix), `EmailTemplateService`, `EmailBackgroundWorker`, `NotificationOutbox` |
| Audit Trail | `AuditLogService` (partial + typed methods), `AuditLog`, `AuditLogDetail`, append-only in `AppDbContext` |
| Knowledge Base | `KBArticlesController`, `KBSuggestController` *(new)*, `KBArticleService` (partial), `KBArticle`, `KBArticleVersion`, `KBSolveEvent` *(new)*, `KBCommentAttachment` *(new)* |
| Reporting | `ReportsController`, `ReportingService` (partial + 9 types), `ReportQueue`, `ReportBackgroundWorker` |
| Recurrence | `RecurringTemplatesController`, `RecurringTemplateService`, `RecurringTicketWorker` |
| Administration | `UsersController`, `DepartmentsController`, `SystemSettingsController`, `CurrentUserService` (extended) |

---

## God Nodes (Updated)

1. **`TicketService`** — ticket lifecycle, SLA, escalation, notification, survey, KB-resolution, typed audit. All 7 operations fully wired.
2. **`AppDbContext`** — schema relationships, audit append-only, notification preference JSON conversion, KB entity constraints.
3. **`ReportingService`** — routes and generates all 9 PRD report types with PDF/CSV export.
4. **`NotificationService`** — complete §5.2 trigger matrix; checks user preferences before every non-mandatory send.
5. **`KBArticleService`** — CRUD, versioning, feedback, search, suggest, attach, solve/dismiss workflow.
6. **`SlaMonitorWorker`** — SLA warning, breach, auto-escalation scans, now uses `SendSlaBreachAsync`.
7. **`UserService`** — user CRUD, import, preferences, welcome/deactivation email via `SendWelcomeEmailAsync`.
8. **`AuditLogService`** — generic + typed methods; all services now use typed variants where PRD §8 specifies field capture.

---

## PRD Coverage Map

| PRD Area | Status | Notes |
|---|---:|---|
| Core ticket lifecycle | ✅ Complete | Create, update, delete (archive), reopen, escalate, KB-resolve all wired |
| Role permissions | ✅ Complete | Admin/Agent/User/DepartmentHead enforced across all endpoints |
| Ticket v2 fields | ✅ Complete | `IsAutoEscalated`, `ResolvedAt`, `ResolutionSummary` added; department fallback logic in place |
| Email notifications | ✅ Complete | Full §5.2 trigger matrix implemented; mandatory events bypass preference checks |
| SLA | ✅ Complete | SLA targets from admin settings; pause/resume, override, breach detection, response DTO fields |
| Reporting | ✅ Complete | All 9 report types; PDF charting via QuestPDF; CSV via CsvHelper; async threshold + 24h download link |
| Audit trail | ✅ Complete | Typed methods for all PRD §8 events; old/new value capture in `AuditLogService.partial.cs` |
| Knowledge Base | ✅ Complete | CRUD, versioning, feedback, search, suggest, attach-to-comment, solve/dismiss flow, `KBSolveEvent` |
| Escalation workflow | ✅ Complete | Manual + auto (SLA breach, reopen limit); `SendEscalationAsync` used throughout |
| CSAT surveys | ✅ Complete | Survey triggered on Resolved/Closed; preference opt-out respected; `IsSurveySent` guard |
| Recurring templates | ✅ Complete | Cron templates, worker, manual trigger, max occurrences, audit logging |
| Departments | ✅ Complete | Unique names, active-user guard, General protection |
| System administration | ✅ Complete | Global settings, SMTP, notification preferences, user import |
| Data retention | ✅ Complete | Soft-delete (archive) pattern; archival worker for age-based policy |

---

## Remaining Gap

| Item | Status |
|---|---|
| Angular frontend KB suggest (debounce + solve/dismiss button) | ⏳ Not started |
| AutoMapper vulnerability upgrade | ⏳ Optional |

---

## Linked Detail
- Full backend graph data: `graph.json` (node/edge snapshot from 2026-05-07, topology unchanged)
- Interactive graph: `graph.html`

To improve the README.md file for the Helpdesk Backend project, we will incorporate the new content while ensuring that the existing structure and information are preserved. Below is the revised README.md file:

# ??? HELPDESK BACKEND

> **AI INSTRUCTION:** Read this document to understand the underlying architecture, data models, patterns, and current state of the Helpdesk API before generating code for upgrades or new features.

---

## 1. TECH STACK & FRAMEWORK
- **Framework:** ASP.NET Core Web API (.NET 8)
- **Language:** C# 12.0
- **Database:** SQL Server
- **ORM:** Entity Framework Core 8
- **Authentication:** ASP.NET Core Identity (Custom `User` mapping to `IdentityUser<int>`) + JWT Bearer Tokens
- **Object Mapping:** AutoMapper
- **Validation:** DataAnnotations

---

## 2. ARCHITECTURAL PATTERNS
The application follows a strict Clean/Layered Architecture:
1. **Controllers (`/Controllers`)**: Expose RESTful endpoints, validate inputs via ModelState, extract user context (JWT claims), and wrap outputs in a standard `ApiResponse<T>`. Inherit from a custom `BaseController`.
2. **Services (`/Services` & `/Interfaces`)**: Contain core business logic, validation rules, Role-Based Access Control (RBAC) checks, and orchestrate persistence.
3. **Unit of Work & Repositories (`/Repositories`)**: Implement the Generic Repository Pattern (`IGenericRepository<T>`) centralizing DB operations via `IUnitOfWork`.
4. **Data (`/Data`)**: `AppDbContext` configuring entity relationships using Fluent API (Strict cascading and restrict delete rules).

---

## 3. GLOBAL PATTERNS & STANDARDS

### Standardized Response (`ApiResponse<T>`)
Every endpoint returns data wrapped in a generic response structure:
{
  "success": true, // or false
  "message": "Operation result description",
  "data": { ... }  // actual payload or null on error
}

### Global Error Handling
Handled centrally by `ExceptionMiddleware`. Maps custom exceptions to HTTP status codes:
- `NotFoundException` -> 404 Not Found
- `ForbiddenException` -> 403 Forbidden
- `ValidationException` -> 400 Bad Request
- Unexpected Exceptions -> 500 Internal Server Error

### Pagination & Filtering
Implemented via `TicketFilterDto` utilizing `PagedResponse<T>` wrapper, providing `Page`, `PageSize`, `TotalCount`, and `TotalPages`.

---

## 4. ENTITIES & DATABASE SCHEMA

### Core Models
- **`User`** (inherits `IdentityUser<int>`): Has `FirstName`, `LastName`, `IsActive`, `CreatedAt`.
- **`Ticket`**: Associated with a Creator, Agent (AssignedTo), RaisedForUser, and Category. Fields: `Title`, `Description`, `Status`, `Priority`.
- **`Category`**: Simple lookup table for ticket categorization.
- **`Comment`**: Belongs to a single Ticket and Author (User).
- **`AuditLog` & `AuditLogDetail`**: Tracks all system events (especially Ticket lifecycle and status changes).

### Entity Relationships
- **User -> Tickets:** 1:N (CreatedBy), 1:N (AssignedTo), 1:N (RaisedFor)
- **Category -> Tickets:** 1:N 
- **Ticket -> Comments:** 1:N (Cascade Delete)
- **Ticket -> AuditLogs:** 1:N (Restrict Delete)
- **AuditLog -> AuditLogDetails:** 1:N (Cascade Delete)

---

## 5. ENUMS
- **`UserRole`**: Admin (3), Agent (2), User (1)
- **`TicketStatus`**: Open (1), InProgress (2), OnHold (3), Resolved (4), Closed (5), Reopened (6)
- **`TicketPriority`**: Low (1), Medium (2), High (3), Critical (4)
- **`AuditEventType` / `AuditEntityType`**: Used to define what was changed and where (e.g., `TicketCreated`, `TicketStatusChanged`).

---

## 6. SERVICES & BUSINESS LOGIC FLOW

### `AuthService`
- Handles Identity login (`UserManager.CheckPasswordAsync`), token generation, and role assignment.

### `UserService`
- Manages User CRUD.
- Maps `UserRole` dynamically from ASP.NET Identity `UserRoles` table.
- Prevents duplicate emails.

### `TicketService`
- Implements strict RBAC in `FilterTicketsAsync` and `GetTicketByIdAsync`:
  - **User**: Only sees own created/raised-for tickets.
  - **Agent**: Only sees assigned/created tickets.
  - **Admin**: Sees everything.
- Modifying tickets calls `CanModify()` (restricted to Admin or Creator).
- Emits events to `AuditLogService` whenever a ticket is created, updated, or deleted.

### `CommentService`
- Links comments to tickets.
- Validates that non-admins can only delete their own comments.

### `DashboardService`
- Calculates aggregated statistics (counts, monthly comparisons, top agents by resolved tickets) securely executed at the database level (`CountAsync`).

### `AuditLogService`
- Standardized logging of actions. Stores Old vs. New values using `AuditLogDetail`.

---

## 7. DTOs (Data Transfer Objects)
Used for data shaping and isolation.
- **Inputs**: Use DataAnnotations (`[Required]`, `[MaxLength]`, `[EmailAddress]`, `[RegularExpression]`) for validation. 
  - *Examples*: `CreateTicketDto`, `UpdateUserDto`.
- **Outputs**: Flat DTOs to avoid circular dependencies. Navigation properties are flattened using AutoMapper (e.g., `Ticket.CreatedByUser.FirstName` -> `TicketResponseDto.CreatedByUserName`).

---

## 8. FUTURE UPGRADE GUIDE (HOW TO ADD FEATURES)

If asking an AI to add a new feature (e.g., "Add File Attachments to Tickets"), follow this architectural flow:

1. **Entity**: Create `Attachment.cs` in `Models/`. Add navigation property to `Ticket.cs`.
2. **DbContext**: Configure relations in `AppDbContext.cs` `OnModelCreating` (e.g., Cascade delete on ticket deletion).
3. **Migration**: Create EF Migration to apply schema.
4. **DTOs**: Add `AttachmentResponseDto`, `UploadAttachmentDto`. Update mapping in `MappingProfile.cs`.
5. **Interface**: Add `IAttachmentService.cs`.
6. **Service**: Implement `AttachmentService.cs`. Apply RBAC (check `CanAccess` / `CanModify` on parent Ticket). Emit `AuditLog` for attachment upload/delete.
7. **Controller**: Add endpoints to `TicketsController` (e.g., `POST /api/tickets/{id}/attachments`) ensuring `[Authorize]` is applied and appropriate HTTP codes are used (201, 204, etc.).
8. **DI**: Register service in `Program.cs` (`AddScoped`).

---

## 9. ADDITIONAL RESOURCES
For further information, consider reviewing the following resources:
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [AutoMapper Documentation](https://automapper.org/)
- [JWT Bearer Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-bearer)

---

## 10. CONTRIBUTING
We welcome contributions! Please read our [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to get involved.

---

## 11. LICENSE
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

In this revised README.md, the new content has been seamlessly integrated into the existing structure, enhancing the document's clarity and providing comprehensive information about the Helpdesk Backend project. Additional sections for resources, contributing, and licensing have been added to ensure completeness.
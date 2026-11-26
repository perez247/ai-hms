# 3. Backend architecture

## 3.1 Solution layout

```text
Hms.Domain
  Common/Entity.cs
  Enums/Enums.cs
  Identity/AppUser.cs, RefreshToken.cs
  Entities/Configuration.cs, Hmo.cs, Patients.cs, Clinical.cs, Pharmacy.cs, Diagnostics.cs, Billing.cs, Fitness.cs, AuditLog.cs
  Billing/BillingCalculator.cs, BillStateMachine.cs

Hms.Application
  Common/            ICurrentUser, IAuditWriter, paging, number generator port, exceptions, validation behavior
  Security/          Permissions + role-permission catalogue
  DTOs/              request/response records grouped by module
  Services/          application ports implemented by Infrastructure
  Features/          one command/query per file, with handler and FluentValidation validator

Hms.Infrastructure
  Persistence/       HmsDbContext, design-time factory, DbSeeder, migrations
  Auth/              JwtOptions, JwtTokenService
  Services/          application service implementations
  Jobs/              ExpiryWorker

Hms.Api
  Program.cs
  Controllers/       grouped by module
  Middleware/        current-user adapter, exception handling
```

The dependency rule is enforced with project references: Domain has no packages, Application references only Domain/MediatR/FluentValidation, Infrastructure references Application, and API references Infrastructure. `IAppDbContext` has been removed. Application handlers depend on application ports; Infrastructure implements those ports with EF Core.

## 3.2 Composition and configuration

`Program.cs` wires:

- Npgsql with snake_case naming
- ASP.NET Identity core + EF stores
- JWT bearer authentication
- one authorization policy per permission
- CORS for the Angular app
- MediatR handlers, FluentValidation validators and the validation pipeline behavior
- scoped application ports and `ExpiryWorker`
- startup migration and seed (controlled by `Database:AutoMigrateAndSeed`)

Configuration keys:

```json
{
  "ConnectionStrings": { "Default": "Host=...;Database=hms;..." },
  "Jwt": {
    "Issuer": "Hms.Api",
    "Audience": "Hms.Web",
    "SigningKey": "at-least-32-characters",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 7
  },
  "Cors": { "Origins": ["http://localhost:4200"] },
  "Database": { "AutoMigrateAndSeed": true }
}
```

## 3.3 Authentication and authorization

- Passwords are verified through `UserManager<AppUser>`.
- JWT includes `sub`, name, email, role, staff role, full name and one `permission` claim per permission.
- `PermissionCatalog.ForRole` maps every role; `Admin` returns the union of all permissions.
- Policies are generated from `PermissionCatalog.All` and require the matching `permission` claim.
- Controllers use `[Authorize(Policy = Permissions.X)]`.
- Refresh tokens are hashed, rotated and revoked.
- Disabling a user sets `IsActive = false`, a lockout and a new security stamp.

**Important split enforced by permissions:**

- Pharmacy has `pharmacy.dispense` and `pharmacy.commit`, and shares the Bills page with Finance (`billing.approve`, `billing.negotiate`, `billing.payment`, `hmo.read`); the account book stays Finance-only.
- Finance has `billing.approve` and `billing.payment`, but not `pharmacy.dispense` or `hmo.priorauth.manage`.
- HMO Office has `hmo.approve`, but not `billing.approve` or `billing.payment`.
- Doctor and HMO Office cannot approve a bill; Pharmacy can approve, but only through the normal payment/HMO gates.
- HR Office has only HR permissions.

## 3.4 Endpoint groups

| Group | Representative endpoints | Permission |
|---|---|---|
| Auth | `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/auth/me` | anonymous / authenticated |
| Dispatch | Every controller action sends one command or query through `IMediator` | policy remains on the controller action |
| Patients | `GET/POST /api/patients`, `POST /api/patients/{id}/renew`, HMO enrollment | `patients.*` |
| Appointments | `GET/POST /api/appointments`, check-in | `appointments.*` |
| Clinical | outpatient encounter, admission, inpatient entry, vitals, prescriptions, investigations, surgery, administration, discharge | `encounters.*`, `vitals.*`, `prescriptions.write`, etc. |
| Pharmacy | queue, prescription, dispense, tariff confirmation | `pharmacy.*` |
| Billing | bills, route HMO, negotiate, payment, approve, account book | `billing.*`, `accountbook.*` |
| HMO | PA list/save, coverage, HMO approve | `hmo.*` |
| Fitness | list/create, mark paid, export | `fitness.*` |
| HR | users, payroll, disable, export | `hr.*` |
| Admin | fees, HMO, contracts, tariff import/export, settings | `admin.*` |
| Audit | search audit log | `audit.read` |
| Reference | HMO, doctor and patient enrollment lookups | `patients.read` |

## 3.5 Services

| Service | Main responsibilities |
|---|---|
| `AuthService` | Login, refresh rotation, permission claims |
| `PatientService` | Search, registration, renewal, HMO attach |
| `AppointmentService` | Slot booking, check-in, appointment fee |
| `ClinicalService` | Outpatient/inpatient encounters, vitals, orders, reports, administration, discharge |
| `PharmacyService` | Prescription lookup, stock check, dispense, tariff confirmation, commit |
| `BillingService` | Bill creation, line pricing, totals, state transitions, payments, adjustments, account book, receivables |
| `HmoService` | PA, coverage, HMO approval |
| `FitnessService` | Certificate lifecycle and export |
| `HrService` | User lifecycle and export |
| `AdminService` | Fees, partners, contracts, tariff CSV, settings |
| `AuditQueryService` | Audit search |
| `ReferenceService` | Lookup data for forms |

All services use transactions where a business action creates several rows (registration + period + bill, admission + encounter + bill, pharmacy dispense + stock + bill).

## 3.6 Billing implementation notes

- `BillingCalculator` is pure domain logic: allocation, line arithmetic, totals, outstanding balance and bed-night count.
- `BillStateMachine` is the only gate for bill status changes.
- `BillingService.AddChargeAsync` resolves the active HMO tariff by service code; if no tariff exists, the line uses cash price and the HMO Office must resolve it.
- `SourceKey` makes charge creation idempotent (`med-dispense:{id}`, `bed-day:{id}`, etc.).
- HMO approval requires tariff confirmation on medication lines and an issued PA.
- Finance approval creates the HMO receivable; `RecordPaymentAsync` updates receivable settlement and the account book.
- Negotiated discounts are stored as `BillAdjustment` rows with Finance approver and reason.
- Unpaid bills can be moved to the debt ledger (`IsDebt`, `DebtOwner`). Self-pay debt belongs to the patient; HMO debt belongs to the HMO company. `RecordPaymentAsync`/`ApproveAsync` set `DebtSettledAt` once the outstanding balance reaches zero.

## 3.7 Background jobs

`ExpiryWorker` runs at startup and every 24 hours:

- marks patients expired when registration expires;
- logs registrations expiring inside 30 days;
- marks contracts expired and expiring (90-day warning);
- marks tariffs expired.

The worker is intentionally in-process. A production deployment should:
- run a single worker instance or use a PostgreSQL advisory lock;
- add an email/SMS notification adapter;
- move jobs to a scheduler if the API scales horizontally.

## 3.8 Error handling

`ExceptionHandlingMiddleware` maps:

- `AppUnauthorizedException` → 401
- `AppForbiddenException` → 403
- `AppNotFoundException` → 404
- `AppConflictException` → 409
- `DomainRuleException` → 409 (billing rule violation)
- unhandled → 500 with correlation trace id

## 3.9 Production hardening backlog

- Use EF execution strategy for retryable transactions, or disable retry and handle transaction retries explicitly.
- Add rate limiting and account lockout alerts.
- Add row-level security or a read replica for reporting.
- Add virus scanning for uploaded tariff/report files.
- Replace simple CSV parsing with a validated import pipeline.
- Add an outbox if email/SMS notifications are introduced.

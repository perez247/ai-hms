# 9. Clean Architecture + CQRS migration (phase 1)

## 9.1 Layer rules now enforced

```text
Web API  ->  Infrastructure  ->  Application  ->  Domain
                 |                    |
                 +---- implements ---+
```

- **Domain** has no package references. It contains entities, enums, `BillingCalculator` and `BillStateMachine`.
- **Application** references only Domain, MediatR and FluentValidation. It contains commands, queries, handlers, validators, DTOs, permission rules and application ports (interfaces).
- **Infrastructure** references Application and owns EF Core, Npgsql, ASP.NET Identity, JWT, background jobs and implementations of Application ports.
- **API** owns controllers, composition root, middleware and the `ICurrentUser` HTTP adapter.

`AppUser` and `RefreshToken` moved from Domain to `Hms.Infrastructure.Identity`. Domain entities no longer expose `AppUser` navigation properties; infrastructure relationships use explicit `HasOne<AppUser>()` configuration.

## 9.2 CQRS structure

Every controller action resolves `IMediator` and sends exactly one command or query.

Examples:

```text
POST /api/patients                       -> CreatePatientCommand
GET  /api/patients/{id}                  -> GetPatientQuery
POST /api/pharmacy/dispenses             -> DispenseCommand
POST /api/billing/bills/{id}/approve     -> ApproveBillCommand
POST /api/hmo/bills/{id}/approve         -> ApproveHmoBillCommand
GET  /api/billing/account-book           -> GetAccountBookQuery
```

Features live under `backend/Hms.Application/Features/<Module>`:

```text
Features/Patients/CreatePatientCommand.cs
Features/Pharmacy/DispenseCommand.cs
Features/Billing/ApproveBillCommand.cs
Features/Hmo/ApproveHmoBillCommand.cs
...
```

Each command/query lives in its own file together with its handler and validator. Handlers return the same DTOs as the previous controllers, preserving response contracts. Large infrastructure services are split into partial files by responsibility (billing approvals/account-book/queries, clinical orders/discharge/helpers, admin tariffs/settings).

## 9.3 Validation pipeline

`ValidationBehavior<TRequest,TResponse>` resolves all `IValidator<TRequest>` instances, runs them before the handler, aggregates failures and throws `RequestValidationException`.

The API middleware converts it to a 400 `ValidationProblemDetails`:

```json
{
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more request fields are invalid.",
  "errors": {
    "BillId": ["'Bill Id' must not be empty."],
    "Request.Amount": ["'Request Amount' must be greater than '0'."]
  },
  "traceId": "..."
}
```

Validation is registered once in `Program.cs`:

```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ValidationBehavior<,>).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

`IFormFile` stays in the Web API. The tariff endpoint reads the upload into a byte array and sends `ImportTariffCommand`, keeping Application free of ASP.NET Core.

## 9.4 Public contract impact

Successful request/response contracts, routes and authorization policies are unchanged.

**Deliberate change:** malformed input now returns **400** with a consistent validation payload. Previously some malformed values reached services and returned 409 (conflict) or 404 (not found). Examples:

| Input | Before | After |
|---|---|---|
| Empty patient names | 409 from service | 400 validation |
| Past appointment time | 409 from service | 400 validation |
| Zero payment amount | 409 from service | 400 validation |
| Empty bill ID | 404/409 | 400 validation |
| Out-of-range vitals | 409 from service | 400 validation |
| Invalid PA context | 409 from service | 400 validation |

This follows the instruction to return consistent validation errors, but it is a status-code change for malformed requests. If strict legacy status codes are required, remove the corresponding rule from the validator and let the service keep raising 409.

**Schema impact:** the namespace/navigation refactor generated `20261121115246_CleanArchitectureRefactor`. It only drops and recreates the user foreign-key constraints with new names; no tables or columns are added or removed. Existing data is preserved.

## 9.5 Package choices

| Package | Version | Why |
|---|---|---|
| MediatR | 12.4.1 | .NET 10-compatible, mature registration pipeline; avoids MediatR 13 licensing changes |
| FluentValidation | 11.11.0 | Compatible with .NET 10 and MediatR 12 |
| FluentValidation.DependencyInjectionExtensions | 11.11.0 | Assembly scanning in the composition root |
| Moq | 4.20.72 | Handler/validator unit tests |

## 9.6 Remaining technical debt

1. **EF-heavy application ports still live in Infrastructure.** Handlers now own the use-case boundary and validation, but existing port implementations (`PatientService`, `BillingService`, etc.) still contain EF Core queries and significant orchestration. The next phase should replace those with repositories/read models and move the remaining rules into Application or Domain.
2. **Read models are not projected at the handler level.** Queries delegate to existing services. This preserves behavior now but does not yet provide optimized CQRS read models.
3. **Domain entities still encode some persistence-friendly shapes** (for example, `FullName` is a computed property). This is acceptable for phase 1.
4. **No transactional pipeline behavior.** Existing services use explicit transactions. A future `TransactionBehavior` should own transaction boundaries.
5. **Validation status change** described above.
6. **Authorization is still controller-level** using permission policies. This is intentional: authorization remains an API concern; command handlers assume an authorized caller.

## 9.7 Verification

```bash
dotnet build Hms.slnx
dotnet test Hms.slnx
```

Current result: 33 unit tests and 3 PostgreSQL Testcontainer integration tests pass, with no build warnings.

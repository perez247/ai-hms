# 8. Deliverable map

| Deliverable | Where it lives |
|---|---|
| 1. Domain model + PostgreSQL schema | `docs/01-domain-model.md`, `Hms.Domain/Entities`, `Hms.Infrastructure/Persistence/Migrations` |
| 2. Workflow specification | `docs/02-workflows.md` |
| 3. Backend architecture | `docs/03-backend-architecture.md`, `Hms.Api/Program.cs`, `Hms.Application/Security/Permissions.cs` |
| 4. Backend code | Controllers in `Hms.Api/Controllers`; services in `Hms.Infrastructure/Services`; billing/state rules in `Hms.Domain/Billing` |
| 5. Frontend architecture | `docs/04-frontend-architecture.md`, `app.routes.ts`, role route files, guards |
| 6. Frontend code | `app.config.ts`, `core/guards.ts`, `features/pharmacy/*`, `layout/app-shell.*` |
| 7. Migration + seed | `docs/05-migrations-and-seed.md`, `Persistence/Migrations`, `Persistence/DbSeeder.cs` |
| 8. Test plan | `docs/06-test-plan.md`, `tests/Hms.UnitTests`, `tests/Hms.IntegrationTests` |
| CQRS/Clean Architecture migration | `docs/09-cqrs-clean-architecture-migration.md` |

## Verification snapshot

At the time of scaffold completion:

- Backend builds with 0 warnings/errors.
- 33 unit tests pass (including handlers, validators and validation pipeline).
- 3 integration tests pass against PostgreSQL Testcontainers.
- Angular production build succeeds.
- Browser smoke test: login, Finance bill list, Pharmacy queue and dispense confirmation.
- Manual HMO flow: Pharmacy dispense → Finance route → HMO PA + approval → Finance co-pay + approval → receivable.
- Manual inpatient flow: Doctor discharge request → bed-day generation → Finance payment/approval → Doctor completion.

## Deliberate scope limits

- One role per user.
- One bill per context, no supplemental bills after commit.
- No external HMO API or claims clearing house.
- CSV, not XLSX, exports in this scaffold.
- In-process expiry alerts, no email/SMS adapter.
- Local Tailwind wrapper layer with Spartan Brain primitives; Helm wrappers to be generated when available.

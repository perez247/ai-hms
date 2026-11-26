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
| Funding, Stocks and result workflows design | `docs/10-funding-stock-and-results-design.md` |
| Stock items and prices management | `docs/11-stock-item-price-management.md` |
| Error handling, visit bill, Finance/Pharmacy edits, stock search | `docs/12-error-billing-finance-pharmacy-search.md` |
| Ongoing-visit appointment block, worklist filter, encounter detail | `docs/13-ongoing-visit-worklist-encounter-detail.md` |
| Patient-owned vitals, Spartan stock pickers, encounter refinements | `docs/14-vitals-spartan-select-encounter-refinements.md` |
| Encounter detail completeness, pharmacy commit gate, Finance HMO read | `docs/15-encounter-detail-pharmacy-commit-finance-hmo-read.md` |
| Encounter completeness, history, debt ledger and disposition flows | `docs/16-encounter-completeness-debt-and-disposition.md` |

## Verification snapshot

At the time of scaffold completion:

- Backend builds with 0 warnings/errors.
- 62 unit tests pass (including handlers, validators and validation pipeline).
- 24 integration tests pass against PostgreSQL Testcontainers; they create their own patients and
  encounters instead of relying on demo seed rows.
- Angular production build succeeds; 14 frontend tests pass.
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

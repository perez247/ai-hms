# Clinic Management System (HMS)

A modular-monolith clinic management scaffold for the complete patient journey: registration, appointments, triage, encounters, pharmacy, HMO authorisation, finance, discharge, HR and administration.

## Stack

- **Backend:** .NET 10 LTS, ASP.NET Core Web API, EF Core 10, Npgsql, PostgreSQL 17
- **Frontend:** Angular 22 standalone components, Spartan Brain (`@spartan-ng/brain`), Tailwind CSS 4
- **Backend pattern:** Clean Architecture + CQRS with MediatR, FluentValidation pipeline
- **Auth:** ASP.NET Identity + JWT access tokens + rotating refresh tokens
- **Database:** PostgreSQL schema `clinic`

## What is implemented

- All eight staff roles plus Admin, with permission policies enforced at API endpoints and Angular route guards.
- Patient registration with 24-month validity, renewal and HMO enrollment.
- Appointment booking and appointment fee billing.
- Outpatient encounter, vitals, prescriptions, investigations, surgeries and reports.
- Inpatient admission with multiple encounter entries, bed-day computation and discharge clearance.
- Pharmacy queue, stock checks, dispensing, HMO tariff application, commit-to-Finance and a shared Bills workbench (approve, negotiate, settle, track debt).
- Billing state machine: draft, committed, HMO pending, HMO approved, Finance approved, paid and void.
- HMO PA capture, line coverage, referral destination and HMO approval.
- Finance payments, negotiated-price audit adjustments, receivables and account book.
- Fitness certificates with flexible JSON fields and CSV export.
- HR user creation, salary/bank updates, account disable and export.
- Admin fee configuration, HMO partner registration, contracts, tariff CSV import/export and settings.
- Append-only audit log for monetary and clinical state changes.
- All API actions routed through `IMediator`; commands/queries have dedicated handlers and FluentValidation validators.
- Domain and Application layers have no ASP.NET Core, EF Core or Identity package references.
- Background expiry worker for registrations, HMO contracts and tariffs.
- EF Core migrations and an idempotent reference-data seed (users, settings, fees, HMO/tariff, stock); no clinical demo records.

## Repository layout

```text
backend/
  Hms.Domain/            entities, enums, billing calculator, bill state machine
  Hms.Application/       commands, queries, handlers, validators, DTOs, permissions, ports
  Hms.Infrastructure/    EF Core, Identity/JWT, application port implementations, jobs, migrations, seed
  Hms.Api/               controllers, middleware, composition root
  tests/
    Hms.UnitTests/       billing, state machine, permission tests
    Hms.IntegrationTests/Testcontainers + HMO approval flow
frontend/hms-web/        Angular standalone app
docs/                    workflow, architecture, seed, test and compliance notes
docker-compose.yml       PostgreSQL 17
```

## Quick start

Prerequisites: .NET 10 SDK, Docker, Node.js 22+.

```bash
docker compose up -d
dotnet tool restore
dotnet run --project backend/Hms.Api/Hms.Api.csproj
```

The API applies migrations and seeds reference data on startup. It listens on **http://localhost:5080**. OpenAPI is at **http://localhost:5080/openapi/v1.json**.

Frontend:

```bash
cd frontend/hms-web
npm install
npm start
```

The web app runs on **http://localhost:4200** and expects the API on port 5080.

## Demo accounts

Password for all seeded accounts: `ChangeMe123!`

| Email | Role |
|---|---|
| admin@clinic.local | Admin |
| receptionist@clinic.local | Receptionist |
| nurse@clinic.local | Nurse |
| doctor@clinic.local | Doctor |
| pharmacy@clinic.local | Pharmacy |
| finance@clinic.local | Finance |
| hmo@clinic.local | HMO Office |
| hr@clinic.local | HR Office |

The seed includes only the accounts above, clinic settings, fees, the demo HMO contract/tariff and the
stock catalogue. It creates **no patients, appointments, encounters, admissions or bills**; register a
patient and book/check in an appointment to exercise the workflows.

## Useful commands

```bash
# all tests
dotnet test Hms.slnx

# API only
dotnet run --project backend/Hms.Api/Hms.Api.csproj

# add a migration
dotnet tool run dotnet-ef migrations add <Name> \
  --project backend/Hms.Infrastructure/Hms.Infrastructure.csproj \
  --startup-project backend/Hms.Api/Hms.Api.csproj \
  --output-dir Persistence/Migrations

# reset database and seed
docker compose down -v
docker compose up -d
dotnet run --project backend/Hms.Api/Hms.Api.csproj
```

## Security notes before deployment

- Replace `Jwt:SigningKey` with a secret from a vault; never use the development key.
- Replace seed passwords and remove demo accounts.
- Put the API behind HTTPS and restrict CORS.
- Review the compliance checklist in `docs/07-compliance.md` with the clinic.
- The audit trail contains PHI; restrict DB access and backups accordingly.

## Scope boundaries

- No microservices, event sourcing or external HMO claims API.
- HMO authorisation is captured manually by the HMO Office; no payer API integration is assumed.
- Tariffs are imported as CSV; the parser is deliberately simple and should be hardened for production files.
- The styled UI layer is local Tailwind components. Spartan Brain is installed and its button primitive is used; the `@spartan-ng/helm` package was not available from the registry during scaffolding, so Helm wrappers were not generated. See `docs/04-frontend-architecture.md`.

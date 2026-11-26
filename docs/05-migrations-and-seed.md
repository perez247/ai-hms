# 5. Migration and seed plan

## 5.1 Migration

The migrations are committed at:

```text
backend/Hms.Infrastructure/Persistence/Migrations/
  20261121024454_InitialCreate.cs
  20261121115246_CleanArchitectureRefactor.cs
  HmsDbContextModelSnapshot.cs
```

`CleanArchitectureRefactor` is a metadata-only migration: it renames the user foreign-key constraints after `AppUser` moved to Infrastructure and navigation properties were removed from Domain entities. It does not add or remove tables or columns.

It creates schema `clinic`, the identity tables, domain tables, check constraints, indexes, filtered unique indexes and human-number sequences.

Apply it:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project backend/Hms.Infrastructure/Hms.Infrastructure.csproj \
  --startup-project backend/Hms.Api/Hms.Api.csproj
```

The API also calls `DbSeeder.SeedAsync`, which runs `MigrateAsync` and seeds only when the `users` table is empty.

## 5.2 Seed accounts

One user per role, password `ChangeMe123!`:

| Role | Email |
|---|---|
| Admin | admin@clinic.local |
| Receptionist | receptionist@clinic.local |
| Nurse | nurse@clinic.local |
| Doctor | doctor@clinic.local |
| Pharmacy | pharmacy@clinic.local |
| Finance | finance@clinic.local |
| HMO Office | hmo@clinic.local |
| HR Office | hr@clinic.local |

## 5.3 Seed configuration

- Registration validity: 24 months.
- HMO contract term: 60 months.
- Currency: NGN.
- Timezone: Africa/Lagos.
- Bed-day mode: per night.
- Expiry warning windows: 30 and 7 days.

Default fees:

| Fee | Amount |
|---|---:|
| Registration new | 10,000 |
| Registration renewal | 7,500 |
| Appointment | 5,000 |
| Emergency | 20,000 |
| Inpatient consultation | 15,000 |
| General bed-day | 12,000 |
| Private bed-day | 30,000 |
| Fitness certificate | 8,000 |

## 5.4 Seeded HMO and tariff

`HealthGuard HMO` (`HEALTH-DEMO`) with a five-year contract and active tariff v1:

- Paracetamol 500mg: 400, 80% covered.
- Amoxicillin 500mg: 1,000, 80% covered.
- Inpatient consultation: 15,000, 100% covered.
- General/private bed-day: 12,000/30,000, 100% covered.
- CBC: 5,000, 100% covered.
- Ultrasound: 15,000, 90% covered.
- Appendectomy: 250,000, 90% covered.

## 5.5 Clinical data is entered through the application

The seed deliberately creates **no** patients, appointments, encounters, admissions, bills or clinical
records. Only users, settings, fees, the HMO/contract/tariff and the stock catalogue are seeded. This
keeps a fresh environment clean and makes the integration tests create exactly the records they assert on.

## 5.6 Manually exercising the workflows

Create your own data first:

1. Login as Receptionist and register a patient (optionally add an HMO enrollment).
2. Book an appointment and check it in. Check-in creates the single visit bill (appointment fee).
3. Login as Doctor and open the row from the encounter worklist; the encounter gets its own page.
4. Order tests/procedures and/or a prescription, save the draft, then sign the encounter.

Outpatient HMO journey:

1. Login as Pharmacy and open `/pharmacy/prescriptions` for the encounter.
2. Dispense the prescribed items. The visit bill commits.
3. Login as Finance and route the visit bill to the HMO Office.
4. Login as HMO Office, record a PA for the encounter, approve the HMO portion.
5. Login as Finance, record the patient co-pay, approve the bill.
6. The bill is `Approved`, with an `Open` receivable for the HMO amount. The patient's appointment is
   now closed, so a new appointment can be booked.

Inpatient discharge journey:

1. Login as Doctor, create an admission and open its inpatient encounter.
2. Request discharge with a summary; this generates bed-days and commits the final bill.
3. Login as Finance, pay the full patient amount, then approve.
4. Login as Doctor and complete discharge.

## 5.7 Reset

```bash
docker compose down -v
docker compose up -d
dotnet run --project backend/Hms.Api/Hms.Api.csproj
```

The seed is idempotent: it skips if any user exists. To re-seed without dropping volumes, truncate the `clinic` schema and restart the API.

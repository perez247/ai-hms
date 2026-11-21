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

## 5.4 Demo HMO and tariff

`HealthGuard HMO` (`HEALTH-DEMO`) with a five-year contract and active tariff v1:

- Paracetamol 500mg: 400, 80% covered.
- Amoxicillin 500mg: 1,000, 80% covered.
- Inpatient consultation: 15,000, 100% covered.
- General/private bed-day: 12,000/30,000, 100% covered.
- CBC: 5,000, 100% covered.
- Ultrasound: 15,000, 90% covered.
- Appendectomy: 250,000, 90% covered.

## 5.5 Demo journeys

### Outpatient — Ada Okafor

- Patient `PAT-DEMO-0001`, HealthGuard enrollment `HG-000123`.
- Appointment `APT-DEMO-0001`, checked in.
- Outpatient encounter `ENC-DEMO-0001`.
- Prescription: paracetamol 9 tablets + amoxicillin 10 capsules.
- Bill `BILL-DEMO-0002` in Draft until Pharmacy dispenses and commits.
- Vitals already recorded.

### Inpatient — Chinedu Bello

- Patient `PAT-DEMO-0002`, self-pay.
- Admission `ADM-DEMO-0001`, general ward, active.
- Inpatient encounter `ENC-DEMO-0002`.
- Bill `BILL-DEMO-0003` in Draft with inpatient consultation line.
- Bed-days are generated when the Doctor requests discharge.
- Discharge completion requires Finance approval.

## 5.6 Manually exercising the demo

Outpatient HMO journey:

1. Login as Pharmacy and open `/pharmacy/prescriptions`.
2. Select Ada Okafor and dispense both items.
3. Login as Finance and route `BILL-DEMO-0002` to the HMO Office.
4. Login as HMO Office, record PA `PA-DEMO-001`, approve the HMO portion.
5. Login as Finance, record the patient co-pay, approve the bill.
6. The bill is `Approved`, with an `Open` receivable for the HMO amount.

Inpatient discharge journey:

1. Login as Doctor, open the admission (use `ADM-DEMO-0001` from the database or your own created admission).
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

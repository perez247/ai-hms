# 14. Patient-owned vitals, Spartan stock pickers and encounter detail refinements

## 1. Vitals belong to the patient, not the encounter

- Removed `Encounter.Vitals` and `Vital.Encounter`/`Vital.EncounterId`. `Patient.Vitals` is now the only
  inverse navigation for `Vital`.
- `HmsDbContext` configures `Vital.Patient` with `.WithMany(x => x.Vitals)`; the old encounter FK and
  `(encounter_id, recorded_at)` index were removed.
- Migration `20261124143410_PatientVitalsRelationship` drops `fk_vitals_encounters_encounter_id` and
  `vitals.encounter_id` (with a reversible `Down`). The previously generated `patient-vitals` migration
  that added an unused `patient_id1` column was removed; no shadow patient column is needed.
- `ClinicalService.GetEncounterAsync` still uses `Include(Patient).ThenInclude(p => p.Vitals)` and maps
  the patient's readings into `EncounterDto.Vitals` (newest first); since `docs/15` the encounter detail
  view shows only the five most recent readings.
- `ClinicalService.RecordVitalsAsync` still requires an open encounter for context and audit, but stores
  only `PatientId` on the new vital. Nurse patient-level vitals were already patient-owned.

**Impact:** encounter detail now lists the five most recent vitals recorded for the patient across
encounters, not just the latest reading. Any future code that needs encounter-scoped vitals must filter
by patient and time.

## 2. Plan section search uses Spartan select

- New `StockSelectComponent` (`features/doctor/stock-select.component.ts`) is built on
  `@spartan-ng/brain/select` + `@spartan-ng/brain/popover` primitives already installed in the project.
- It keeps the same data source (`/api/reference/stock-items` with `kind`/`search`) and the existing
  300 ms debounce in `ClerkingFormComponent`; typing in the panel emits `queryChange`, and selecting an
  option emits the `CatalogOption`.
- Selection populates the same values as the old datalist: display `CODE · Name` in the control and the
  stock item id in the save payload.
- The datalist elements for medications, tests and procedures were removed.

**Assumption:** the project only has `@spartan-ng/brain` installed (no HLM select wrapper package), so a
small custom component using the official brain directives is used instead of adding a new dependency.

## 3. Clerking form hidden for closed encounters

- `EncounterDetailComponent.showClerking` is a computed signal based on the current encounter status;
  `Completed` and `Cancelled` hide `<app-clerking-form>` entirely. The SOAP summary remains read-only.
- Other statuses (including `Open`) keep the form visible and editable. Because the visibility is
  computed from the loaded encounter, it updates whenever the status changes.

## 4. Send to Pharmacy removed

- Removed the **Send to pharmacy** link from the doctor's encounter detail page. The pharmacy module and
  its routes were not changed.

## 5. Primary diagnosis is free text

- The Assessment section now uses a plain text input bound to `primaryDiagnosis` instead of an ICD-10
  datalist. No code lookup, validation or code-specific storage remains in the encounter module:
  `ICD10_OPTIONS`, `DiagnosisOption`, `DiagnosisEntry` and the secondary-diagnosis picker were removed.
- `SaveEncounterRequest.Diagnosis` and `Encounter.Diagnosis` were already plain strings, so the backend
  needs no change; existing stored diagnosis text is preserved.

**Impact:** secondary/differential diagnosis capture was removed from the clerking form because it was
ICD-10-only and was never persisted in `SaveEncounterRequest`.

## 6. Files changed

- `backend/Hms.Domain/Entities/Clinical.cs`
- `backend/Hms.Infrastructure/Persistence/HmsDbContext.cs`
- `backend/Hms.Infrastructure/Persistence/Migrations/20261124143410_PatientVitalsRelationship.cs` (+ designer)
- `backend/Hms.Infrastructure/Persistence/Migrations/HmsDbContextModelSnapshot.cs`
- `backend/Hms.Infrastructure/Services/ClinicalService.cs`, `ClinicalService.Orders.cs`
- `backend/tests/Hms.IntegrationTests/NurseAndLedgerFlowTests.cs`
- `frontend/hms-web/src/app/features/doctor/stock-select.component.ts` (new)
- `frontend/hms-web/src/app/features/doctor/clerking-form.component.ts`, `clerking-form.component.html`
- `frontend/hms-web/src/app/features/doctor/clerking.models.ts`
- `frontend/hms-web/src/app/features/doctor/encounter-detail.component.ts`, `encounter-detail.component.html`
- `docs/08-deliverables.md`, this note

## 7. Verification

- `dotnet test Hms.slnx`: 62 unit + 25 integration tests pass (new test asserts patient vitals appear on
  the encounter detail payload).
- `npm run build`: green. `npm test -- --watch=false`: 14 tests pass.
- Reset the local database after pulling the migration so the removed `encounter_id` column is dropped.

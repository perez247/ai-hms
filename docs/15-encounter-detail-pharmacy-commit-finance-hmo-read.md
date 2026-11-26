# 15. Encounter detail completeness, pharmacy commit gate and Finance HMO read

## 1. Encounter detail shows the five most recent patient vitals

- `ClinicalService.GetEncounterAsync` still reads `Patient.Vitals` (vitals are patient-owned), orders by
  `RecordedAt` descending and now returns at most five readings in `EncounterDto.Vitals`.
- `EncounterDetailComponent.latestVitals` also slices the first five, so the Objective table stays capped
  even if the payload grows again.
- The vitals table shows temperature, blood pressure, pulse, respiratory rate, SpO₂, pain score,
  weight/height and the recorded notes, newest first.

## 2. Encounter detail shows every prescription line

- `PrescriptionSummaryDto` now carries `Items` (`PrescriptionItemSummaryDto`: medication name, dose,
  route, frequency, duration days, prescribed/dispensed quantity and instructions).
- `GetEncounterAsync` includes `Prescriptions → Items → StockItem` and maps every prescription on the
  encounter, newest first; `CreatePrescriptionAsync` returns the same shape.
- The Plan & Orders table therefore renders medication name, dosage, route, frequency and duration for
  every line on every prescription.

## 3. All remaining clerking fields are persisted and displayed

- New `Encounter.ClerkingDetails` (`text`) stores the ancillary form fields that have no dedicated
  column: ROS notes, allergies, general appearance and notes, point-of-care results, diagnosis
  certainty, clinical impression, patient advice, follow-up date and referral details.
- New `Encounter.PresentingComplaintDurationDays` stores the symptom duration normalised to whole days
  (hours round up, weeks × 7, months × 30).
- `SaveEncounterRequest` accepts `Clerking` and `PresentingComplaintDurationDays`; the request validator
  bounds the optional text/list fields.
- `encounter-detail.component.html` renders every one of these fields, plus normal and abnormal
  examination findings (the `Normal` flag is now stored on each `ExaminationItemDto`).
- The presenting complaint shows `· N day(s)` only when a duration was recorded; otherwise nothing
  extra is rendered.
- Migration `20261124215247_EncounterClerkingDetails` adds both columns.

## 4. Pharmacy may only commit eligible bills to Finance

- `PharmacyPrescriptionDto` exposes `CanCommitToFinance` and `CommitBlockedReason`.
- Outpatient encounters are eligible only when `EncounterStatus.Completed` (the doctor has signed).
  Inpatient admissions are eligible only when `AdmissionStatus.Discharged`.
- `PharmacyService.DispenseAsync` rejects a blocked outpatient commit with `409 Conflict` and the same
  reason returned in the DTO, so the rule cannot be bypassed through the API.
- Inpatient ward dispensing remains available while the patient is admitted; those charges stay on the
  draft admission bill and are committed at discharge. The UI shows a clear “waits for discharge”
  message instead of hiding that behaviour.
- `PharmacyComponent` disables the Commit button, guards the confirm dialog and displays the block
  reason for outpatient draft encounters.

## 5. Finance can read HMO prior authorizations

- `PermissionCatalog` now grants `Permissions.HmoRead` to `StaffRole.Finance`; the existing
  `[Authorize(Policy = Permissions.HmoRead)]` on `HmoController.Search` therefore admits Finance users.
- `PermissionCatalogTests` and `HmoBillingFlowTests` assert the permission and the endpoint response.

## 6. Files changed

- `backend/Hms.Domain/Entities/Clinical.cs`
- `backend/Hms.Infrastructure/Persistence/HmsDbContext.cs`
- `backend/Hms.Infrastructure/Persistence/Migrations/20261124215247_EncounterClerkingDetails.cs` (+ designer, snapshot)
- `backend/Hms.Application/DTOs/ClinicalDtos.cs`, `PharmacyDtos.cs`
- `backend/Hms.Application/Features/Clinical/SaveEncounterCommand.cs`
- `backend/Hms.Application/Security/PermissionCatalog.cs`
- `backend/Hms.Infrastructure/Services/ClinicalService.cs`, `ClinicalService.Orders.cs`, `PharmacyService.cs`
- `backend/tests/Hms.UnitTests/PermissionCatalogTests.cs`
- `backend/tests/Hms.IntegrationTests/HmoBillingFlowTests.cs`,
  `DoctorEncounterListAndClerkingTests.cs`, `NurseAndLedgerFlowTests.cs`,
  `PharmacyCommitEligibilityTests.cs` (new), `TestData.cs`
- `frontend/hms-web/src/app/core/models.ts`
- `frontend/hms-web/src/app/features/doctor/encounter-detail.component.ts`, `.html`
- `frontend/hms-web/src/app/features/doctor/clerking-form.component.ts`, `clerking.models.ts`
- `frontend/hms-web/src/app/features/pharmacy/pharmacy.component.ts`, `.html`
- `docs/08-deliverables.md`, this note

## 7. Verification

- `dotnet test Hms.slnx`: 62 unit tests and 30 integration tests pass.
- `npm run build` is green; `npm test -- --watch=false` runs 14 tests.
- Reset the local database after pulling the new migration so both encounter columns exist.

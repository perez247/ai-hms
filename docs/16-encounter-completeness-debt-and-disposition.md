# 16. Encounter completeness, history, debt ledger and disposition flows

This note covers the follow-up batch of encounter, pharmacy, appointment, debt and disposition changes.
It complements `docs/02-workflows.md`, `docs/03-backend-architecture.md` and `docs/06-test-plan.md`,
which were updated for the debt ledger in the same batch.

## 1. Plan rows are optional but every supplied row must be complete

- `SaveEncounterCommandValidator` validates each medication, test and procedure row in the request. A
  row may be omitted, but if it is present it needs a stock item, and medication rows also need dose,
  route, frequency, duration (≥ 1 day) and quantity (> 0). Test/procedure rows need a stock item and a
  valid priority.
- The same rules run for draft saves and signing, so a partially-filled row can never be stored. The
  clerking form mirrors the rules and highlights the offending row before it sends the request.
- Medications are not required to sign: an encounter with no prescription rows signs normally and
  creates no prescription. Tests and procedures follow the same optional-row rule.
- The medication row now carries an explicit `Quantity`; draft edits no longer recompute it from the
  duration.

## 2. Draft encounters stay editable and round-trip into the form

- `SaveEncounterAsync` rejects `Completed` and `Cancelled` encounters with `409`, but leaves `Open`
  encounters fully editable.
- Plan orders are reconciled by row id instead of appended: existing medication, test and procedure
  rows are updated in place, new rows are inserted, and rows missing from the payload are removed.
  `null` collections mean “leave the orders untouched”; an empty list clears them.
- Removals are blocked once a result exists or a bill line references the order, and dispensed
  medication lines cannot be edited or removed.
- `GetEncounterAsync` returns the same rows, so reopening a draft shows the saved values in the clerking
  form rather than a blank form.

## 3. Pharmacy can work the Bills page

- `PermissionCatalog` grants Pharmacy `BillingApprove`, `BillingNegotiate`, `BillingPayment` and
  `HmoRead` in addition to its pharmacy permissions.
- The account book remains Finance-only; Pharmacy cannot read or post account-book entries.
- The Bills page therefore supports approve, negotiate and settle for Pharmacy through the normal
  payment and HMO gates.

## 4. Paid appointments leave the nurse vitals queue

- `NurseService.GetScheduledAppointmentsAsync` drops an appointment when its non-void visit bill is
  `Paid`, `Approved`, `HmoPending` or `HmoApproved`, or when the bill has any paid amount.
- The nurse tab is labelled **Scheduled appointments awaiting vitals** and explains why paid visits are
  absent.

## 5. Bills can be moved to the debt ledger

- `Bill` gains `IsDebt`, `DebtOwner` (`Patient` or `Hmo`), `DebtReason` and `DebtSettledAt`.
  Migration `20261126011537_BillDebtTracking` adds the columns.
- `POST /api/billing/bills/{id}/debt` (`MoveBillToDebtCommand`) records the owner (patient for
  self-pay, HMO company for an HMO bill) and reason. `IsDebt` is a flag, not a new `BillStatus`, so the
  approval/payment state machine is unchanged.
- `GET /api/billing/bills?inDebt=true` returns active debt; the default list excludes it.
  `RecordPaymentAsync` and `ApproveAsync` set `DebtSettledAt` when the outstanding balance reaches zero,
  which removes the bill from the active debt view.
- The Bills page has **Bills** and **Debt** tabs, an **Owed by** column, a debt badge/banner and a
  **Move to debt** action.

## 6. Previous encounters show the full SOAP record and paginated results

- `EncounterHistoryItemDto` now carries `HistoryOfPresentingComplaint`, `Examination`, `Clerking`,
  `Medications`, `Orders` (`EncounterPlanOrderDto`) and `ResultCount`.
- The prior-encounter query batch-loads prescriptions, service requests and result counts, so the
  history accordion renders Subjective, Objective, Assessment, Plan and the number of results without
  embedding every result.
- Results are fetched on demand from `GET /api/clinical/encounters/{id}/results?page=&pageSize=`
  (`GetEncounterResultsQuery`), which returns `PagedResult<ServiceRequestSummaryDto>`. The detail page
  shows Previous/Next controls and a page counter.

## 7. New encounters are created from the encounter detail page

- The **Start a new encounter or admission** block was removed from the doctor encounters page, along
  with its now-unused component state and methods.
- An **Add New Encounter** button sits at the top-right of the encounter detail header and is rendered
  only for `Inpatient` encounters with an admission.
- Clicking it signs the current encounter first when it is still open (the embedded clerking form’s
  `sign()` path runs the same validation and save as the normal Sign button). Once the save succeeds,
  or immediately when the encounter is already completed/cancelled, the page calls
  `POST /api/clinical/admissions/{admissionId}/encounters` and navigates to the new encounter.
- If the admission is already awaiting discharge or discharged, the API returns `409` and the global
  API error handler shows the reason.

## 8. Disposition drives admission and discharge

- Signing an **outpatient** encounter with disposition **Admit** creates an admission (ward
  `General`, no bed) and its first inpatient encounter, reusing an existing `Active` admission when
  one is already open. An admission that is `PendingDischarge` blocks a second admission with `409`.
- Signing an **inpatient** encounter with disposition **Discharge** calls the existing discharge
  request path: bed-days are generated, the visit bill is committed to Finance, and the admission moves
  to `PendingDischarge` with a generated discharge summary. Finance clearance still completes the
  discharge.
- All encounters under one admission now share the admission’s visit bill
  (`BillingService.GetOrCreateVisitBillAsync` looks up `BillKind.Visit` by `AdmissionId` first), so
  bed-days, ward medication and later reviews bill to the same account.
- The clerking form labels the options **Admit (outpatient → inpatient)** and **Discharge (close
  treatment & start billing)** and explains the effect under the select.

## 9. Files changed

- Backend: `Domain/Enums/Enums.cs`, `Domain/Entities/Billing.cs`, `Domain/Entities/Clinical.cs`,
  `Application/DTOs/BillingDtos.cs`, `ClinicalDtos.cs`, `Application/Features/Clinical/*`,
  `Application/Features/Billing/MoveBillToDebtCommand.cs`,
  `Infrastructure/Services/ClinicalService*.cs`, `BillingService*.cs`, `NurseService.cs`,
  `Infrastructure/Persistence/Migrations/20261126011537_BillDebtTracking.*`, `HmsDbContext*`,
  `Application/Security/PermissionCatalog.cs`, `Hms.Api/Controllers/*`
- Frontend: `core/models.ts`, `core/api.service.ts`, `features/doctor/doctor.component.ts`/`.html`,
  `features/doctor/encounter-detail.component.ts`/`.html`,
  `features/doctor/clerking-form.component.ts`/`.html`, `clerking.models.ts`,
  `features/billing/*`, `features/nurse/*`, `features/pharmacy/*`
- Tests: `DoctorEncounterListAndClerkingTests`, `NurseAndLedgerFlowTests`, `NewWorkflowFeaturesTests`,
  `FundingStockAndResultsTests`, `PermissionCatalogTests`
- Docs: `README.md`, `docs/02-workflows.md`, `docs/03-backend-architecture.md`,
  `docs/06-test-plan.md`, `docs/08-deliverables.md`, this note

## 10. Verification

- `dotnet test Hms.slnx`: 68 unit and 41 integration tests pass.
- `npm run build`: green. `npm test -- --watch=false`: 19 tests pass.
- Live API check (`/tmp/opencode/verify_disposition.py`): outpatient Admit creates an admission and an
  inpatient encounter; Add New Encounter creates another inpatient encounter on the same admission;
  inpatient Discharge sets `PendingDischarge`, records a discharge summary and moves the visit bill out
  of `Draft`.
- Live UI check: **Add New Encounter** renders on an inpatient encounter and is absent on an outpatient
  encounter; clicking it posts to the admission’s encounters endpoint.

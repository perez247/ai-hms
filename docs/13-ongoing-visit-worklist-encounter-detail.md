# 13. Ongoing-visit appointment block, doctor worklist and encounter detail page

## 1. Blocking a second appointment during an ongoing visit

- `AppointmentService.CreateAsync` now checks for an open appointment before booking. An appointment is
  open unless it is `Cancelled`/`NoShow` or its non-void visit bill is `Paid`, `Approved`, `HmoPending`
  or `HmoApproved`.
- The guard is server-side, so direct API calls are blocked too. The conflict message names the blocking
  appointment number, date and status, and explains that the visit bill must be paid or invoiced to the
  HMO.
- `GET /api/appointments/ongoing?patientNumber=...` (`AppointmentsRead`) returns the blocking appointment
  or `null`; Reception uses it on patient-number blur to show a **Booking blocked** panel and disable the
  submit button before the request is sent. A rejected request also shows the server detail in the same
  panel.
- No new status or column was added. `BillStatus` and `AppointmentStatus` remain the source of truth.
- **Assumption:** one open appointment per patient is intended, including a future `Scheduled` one. A
  `Completed` appointment with an unpaid visit bill still blocks until the bill is settled.
- **Conflict:** none. A `Void` bill is ignored so it cannot close an appointment accidentally.

## 2. Doctor encounter worklist defaults to pending and open

- `DoctorComponent` defaults `filterStatus` to `active`, which shows `Pending` queue rows plus `Open`
  encounters.
- The status select offers **Pending & open (default)**, **Completed & cancelled**, **All statuses** and
  the individual statuses, so older encounters remain reachable.
- Rows no longer open inline: clicking a row (or **Open**) navigates to `/doctor/encounters/:id`. A
  pending row still creates its encounter first and then navigates to the new encounter page.
- The old `?encounterId=...` deep link is redirected to the new page.

## 3. Encounter detail page

- New route `doctor/encounters/:id` and component `EncounterDetailComponent`.
- Shows patient/encounter header, S/O/A/P (complaint, HPI, past history, family history, vitals,
  examination, diagnosis, disposition), prescriptions, ordered tests/procedures, and **Returned results**
  (result status, text, findings, abnormal flag, performed time). Results stay visible after the
  encounter is signed.
- The existing structured clerking form is embedded on the page and locked when the encounter is
  completed/cancelled.
- **Previous encounters** render in a separate accordion, newest first, excluding the current encounter.
  The current encounter is only in the page body, keeping it visually distinct from history.

## 4. Seed and test data

- `DbSeeder` now seeds only users, settings, fees, the HMO/contract/tariff and the stock catalogue. It
  creates no patients, appointments, encounters, admissions, bills or clinical rows.
- Integration tests use `TestData.cs` to register their own patients and create appointments, encounters,
  admissions, prescriptions and visits through the public API.
- `BillingService.GetOrCreateVisitBillAsync(Encounter)` now links the check-in visit bill to the encounter
  (`EncounterId`), so HMO prior authorizations and the encounter page resolve the same bill.

## 5. Changed files

New:

- `backend/Hms.Application/Features/Appointments/GetOngoingAppointmentQuery.cs`
- `frontend/hms-web/src/app/features/doctor/encounter-detail.component.ts`
- `frontend/hms-web/src/app/features/doctor/encounter-detail.component.html`
- `backend/tests/Hms.IntegrationTests/TestData.cs`

Modified (this task):

- `backend/Hms.Application/DTOs/AppointmentDtos.cs`, `Services/IAppointmentService.cs`
- `backend/Hms.Infrastructure/Services/AppointmentService.cs`, `BillingService.VisitBill.cs`
- `backend/Hms.Application/DTOs/ClinicalDtos.cs`, `backend/Hms.Infrastructure/Services/ClinicalService.cs`
- `backend/Hms.Api/Controllers/AppointmentsController.cs`
- `backend/Hms.Infrastructure/Persistence/DbSeeder.cs`
- `frontend/hms-web/src/app/core/models.ts`, `core/api.service.ts`
- `frontend/hms-web/src/app/features/reception/appointments.component.ts`
- `frontend/hms-web/src/app/features/doctor/doctor.component.ts`, `doctor.component.html`,
  `doctor.routes.ts`
- Integration tests: `DoctorEncounterListAndClerkingTests`, `FundingStockAndResultsTests`,
  `HmoBillingFlowTests`, `NewWorkflowFeaturesTests`, `NurseAndLedgerFlowTests`,
  `PatientDemographicsTests`, `PatientNumberAndFitnessFlowTests`
- `docs/05-migrations-and-seed.md`, `docs/08-deliverables.md`

No migration is required: the block uses the existing `bills` and `appointments` columns, and the prior
`VisitBillAndFinanceLines` migration already provides the single visit bill.

## 6. Manual verification

1. Drop/recreate the local database (or clear the `clinic` schema) and start the API. Confirm no patients
   or encounters exist.
2. Login as Receptionist, register a patient, book an appointment, and check it in.
3. Try booking a second appointment for the same patient. The form should show **Booking blocked** with
   the first appointment number, and the API should return `409` if called directly.
4. Login as Doctor, open `/doctor/encounters`. Only pending/open rows should show by default; switch the
   status filter to **Completed & cancelled** to see older ones.
5. Open a row. It should navigate to `/doctor/encounters/:id`, show the full SOAP data, and list the
   patient's earlier encounters in the **Previous encounters** accordion, newest first.
6. Order a test, sign the encounter, then submit a result from the Lab screen. Reopen the encounter page
   and confirm the returned result appears under **Returned results**.
7. Pay or route the visit bill to the HMO, then confirm a new appointment can be booked for that patient.

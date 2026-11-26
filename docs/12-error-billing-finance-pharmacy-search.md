# 12. Error handling, single visit bill, Finance/Pharmacy edits and stock search

## 1. Global validation error handling

- `core/api-error-handler.service.ts` maps any HTTP failure to one user-facing message: first message of
  the first entry in `errors`, else `detail`, else a generic fallback. `traceId` is written to the console
  only. `core/api-error.interceptor.ts` routes every API failure through it; the 77 duplicated
  `toast.error(err?.error?.detail ?? ...)` call sites were removed from components.
- **Assumption:** when several fields fail, the first field's first message is the most useful one.
- **Conflict:** none — the backend already emits the RFC 7807 `errors` object from
  `RequestValidationException`.

## 2. Single bill per patient visit

- Added `BillKind.Visit`. Booking no longer creates a bill; check-in creates the visit bill (appointment
  fee included) via `BillingService.GetOrCreateVisitBillAsync`. All service charges (prescriptions,
  tests, procedures, medication administration, bed-days) now target that one bill. Registration remains
  its own bill.
- `AddConsultationChargeAsync` and all consultation bill lines were removed; the finance bill detail and
  manual-bill default category no longer mention consultation. Walk-in/emergency encounters get a visit
  bill scoped to the encounter/admission.
- **Assumption:** the appointment fee is part of the visit bill, and the bill stays `Draft` until the
  existing commit/approval flow runs.
- **Conflict:** the old flow created the appointment bill at booking and a separate encounter/admission
  bill (plus a consultation line). Those paths were replaced, not layered on top.

## 3. Finance visibility and bill editing

- Removed `procedures.requests.read` and `surgeries.write` from Finance; the procedure queue is hidden
  from Finance routes and navigation.
- Added `POST /api/billing/bills/{id}/lines` and `DELETE .../lines/{lineId}` (policy `billing.approve`).
  The finance bill detail now offers an item picker and a remove action.
- **Assumption:** bill lines may only be added/removed while the bill is `Draft` or `Committed`; prices
  come from the payer's `stock_item_prices` row (or HMO tariff when available). Approved/Paid bills are
  immutable.

## 4. Pharmacy prescription adjustments

- Added `PUT /api/pharmacy/prescriptions/{prescriptionId}/items/{itemId}/quantity` with
  `PharmacyDispense`; pharmacy UI exposes +/− controls.
- `StockItem.IsActive` is now editable from the stocks screen; disabled items remain in the stock list
  with a **Disabled** badge but are excluded from `/api/reference/stock-items`, so they can no longer be
  selected in encounters.
- **Assumption:** the prescribed quantity can be lowered only down to the quantity already dispensed;
  stock quantity itself is separate and unchanged by this adjustment.

## 5. Doctor search-as-you-type

- `/api/reference/stock-items` gained a `search` query parameter; it returns only active, name/code
  matching items. The clerking form no longer loads the full catalogue; each picker debounces input by
  300 ms, requires at least two characters, and shows loading/empty states.
- **Assumption:** two characters is the minimum useful term; matching is case-insensitive name-or-code
  and capped server-side.
- **Note:** datalist option values are item codes for all three pickers; change handlers now resolve by
  code (previously tests/procedures resolved by name, so choosing a suggestion did not set the id).

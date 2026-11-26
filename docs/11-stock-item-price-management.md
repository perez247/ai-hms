# 11. Stock items and stock item prices — design and delivery

> **Status: IMPLEMENTED.** The schema, API, permissions, UI and import logic described below were built
> on top of `docs/10-funding-stock-and-results-design.md`. Conflicts C1–C3 were resolved as stated.

---

## 1. Model

| Entity | Fields | Notes |
|---|---|---|
| `stock_item` | `id`, `code` (unique), `name`, `type` (`Medication`/`Test`/`Procedure`), `quantity` (numeric 18,3, ≥ 0), plus existing descriptive fields | The physical product. Quantity lives **on the item**, not in a separate balance table. |
| `stock_item_price` | `id`, `stock_item_id` FK, `payer_id` (uuid), `price` (numeric 14,2, ≥ 0), plus existing effective dates / coverage / PA fields | One current price per item per payer. `payer_id` is either the sentinel **Private** id or an `hmo_companies.id`. |

**Payer category model (Private as the default HMO):**

- `Payers.PrivateId` = a fixed sentinel UUID (`11111111-1111-1111-1111-111111111111`).
- `GET /api/payers` returns `[{ id: PrivateId, name: 'Private', isPrivate: true }, ...active HMO companies]`.
- Every patient implicitly belongs to Private; no enrollment row is required. HMO membership still comes
  from `patient_hmo_enrollments`.
- **Billing is the only difference:** Private prices produce `PayerType.SelfPay` lines billed to the
  individual patient; HMO prices produce `PayerType.Hmo` lines. No "Private HMO company" row is created,
  because that would pollute HMO lists, contracts, tariffs and PA workflows. *(Assumption A1.)*

---

## 2. Schema changes vs current code

| Change | Reason |
|---|---|
| Add `stock_items.quantity` (default 0) | Requirement puts quantity on the item. |
| Rename/replace `item_prices` with `stock_item_prices`, add `payer_id`, keep `price`; unique `(stock_item_id, payer_id)` | Direct per-payer price entity, independent of item creation. |
| Drop `stock_balances` (data migrated into `stock_items.quantity`) | Single source of quantity; avoids two competing stores. |
| Keep `stock_movements` (FK to `stock_items`) | Stock history remains append-only. |
| Keep extra price columns (`effective_from/to`, `coverage_percent`, `requires_prior_authorization`) | Backward compatibility with HMO billing; they are optional metadata on the price row. *(Assumption A2.)* |

---

## 3. API endpoints

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/api/payers` | `stock.read` | Payer categories (Private + HMOs) for filters and price forms. |
| GET | `/api/stocks?type=&search=&payerId=` | `stock.read` | List items; `payerId` filters the returned prices to Private or one HMO. |
| POST | `/api/stocks` | `stock.manage` | Create item (code, name, type, quantity). No price required. |
| PUT | `/api/stocks/{id}` | `stock.manage` | Edit code, name, type, quantity, active flag. |
| POST | `/api/stocks/{id}/prices` | `stock.manage` | Create **or update** a price for a payer (upsert). |
| GET | `/api/stocks/template?kind=items\|prices&format=csv\|xlsx` | `stock.read` | Downloadable upload template. |
| POST | `/api/stocks/import/items` | `stock.manage` | Multipart CSV/XLSX bulk item upload. |
| POST | `/api/stocks/import/prices` | `stock.manage` | Multipart CSV/XLSX bulk price upload. |
| GET | `/api/reference/stock-items?payerId=` | `patients.read` | **Read-only** orderable-item lookup for the doctor's encounter form. |

**Permission changes:** `stock.read` / `stock.manage` are granted only to **Pharmacy** and **Admin**
(Admin via the union). The read-only reference endpoint keeps `patients.read` so doctors can still pick
items during an encounter.

---

## 4. UI changes

- `/stocks`: route guard `roles: ['Pharmacy','Admin']`, permission `stock.read`. Admin-only mutation
  controls also check `stock.manage`; Pharmacy may manage prices and stock but Admin owns item master data
  *(Assumption A3 — current code grants Pharmacy `pharmacy.stock.manage`; if the clinic wants Pharmacy to
  create items too, grant `stock.manage` to Pharmacy as well)*.
- Payer filter dropdown ("All payers", "Private", each HMO) drives `payerId` and the price column shown.
- Item form exposes code, name, type and quantity; price form is a separate "Add / update price" panel
  per item and payer, usable without editing the item.
- Bulk upload panel: template downloads, file input, validation error table with row/column/message.
- HMO profile → **Negotiated medication tariffs**: read-only table filtered to the viewed HMO. Remove any
  "Add stock item" / "Add stock item price" actions from this section; management happens only in `/stocks`.
- Doctor's clerking form continues to use `/api/reference/stock-items`, not `/api/stocks`.

---

## 5. CSV / Excel import logic

**Templates**

| Kind | Columns (header row) |
|---|---|
| items | `code,name,type,quantity` |
| prices | `itemCode,payerIdOrName,price` |

Templates are generated server-side: CSV as text, XLSX as a minimal OOXML workbook (no external NuGet
required) using `System.IO.Compression` + `XDocument`. The first line contains headers and two example rows.

**Parsing** — `StockImportService`:
1. Accept multipart upload (`IFormFile`) with `.csv`, `.xlsx` extensions; reject others with a clear message.
2. CSV: quoted-field-aware parser. XLSX: read `xl/sharedStrings.xml` and `xl/worksheets/sheet1.xml`.
3. Normalise header names (case/space insensitive), require all expected columns.
4. Validate **every row**, accumulating `(rowNumber, column, message)` rather than failing fast.
5. Item rows: code/name required, type must be Medication/Test/Procedure, quantity ≥ 0, duplicate codes
   reported. Existing codes update the item (idempotent upsert).
6. Price rows: item code must resolve to an existing item, payer must resolve to Private or an active HMO
   (by id or name), price ≥ 0. Existing `(item, payer)` price is updated.
7. All valid rows are applied in one transaction; invalid rows are skipped and returned in the response:
   `{ totalRows, imported, errors: [{ row, column, message }] }`.
8. Audit write with counts and file name.

---

## 6. Assumptions (flag for confirmation)

| ID | Assumption / conflict | Current behaviour | Proposed resolution |
|---|---|---|---|
| A1 | Private as default HMO | Private is currently the *absence* of an HMO (`FundingSource.Private`, no company row). | Sentinel payer id + `/api/payers`; no fake HMO company. Billing keeps `SelfPay`. |
| A2 | Price columns | `item_prices` is effective-dated with coverage % and PA flag used by HMO billing. | Keep as optional metadata; one current row per `(item, payer)`. |
| A3 | Pharmacy item-master rights | Pharmacy already has `pharmacy.stock.manage`; Admin has all. | **Resolved:** both Pharmacy and Admin now hold `stock.manage`, so either can create/edit items and prices. |
| **C1** | **Conflict:** "stock visibility restricted to Pharmacy/Admin" vs doctors ordering from Stocks | Doctor's clerking form calls `/api/stocks` with `stock.read`. | Split management (`/api/stocks`, Pharmacy/Admin) from read-only ordering lookup (`/api/reference/stock-items`, `patients.read`). |
| **C2** | **Conflict:** quantity currently lives in `stock_balances` | Pharmacy dispense mutates `StockBalance`. | Migrate quantity into `stock_items.quantity`; update dispense/receipt code; drop `stock_balances`. |
| **C3** | **Conflict:** HMO view currently surfaces tariff management | `HmoProfile` may allow adding tariffs. | Make negotiated tariffs read-only in the single-HMO view. |
| A4 | Excel dependency | No XLSX library in the backend. | Implement minimal OOXML reader/writer in-process; no new package. |
| A5 | Payer naming in uploads | No stable HMO names guarantee. | Accept payer id (GUID) or exact HMO name; Private aliases: `Private`/`SelfPay`/sentinel id. |

# 4. Frontend architecture

## 4.1 Application structure

```text
frontend/hms-web/src/app/
  app.config.ts                 HttpClient, router, auth interceptor, session initializer
  app.routes.ts                 public login + lazy role route groups
  core/
    api.config.ts               API base URL
    auth.service.ts             signals, session storage, permissions
    auth.interceptor.ts         Bearer token + 401 logout
    guards.ts                   authGuard, accessGuard
    api.service.ts              typed API facade grouped by module
    models.ts                   API contracts
  layout/
    app-shell.component.ts/html sidebar, permission-filtered navigation
  features/
    auth/login.component
    home/home.component
    reception/{patients,appointments}
    nurse/nurse.component
    doctor/doctor.component
    pharmacy/pharmacy.component
    finance/{bills,account-book}
    hmo/hmo.component
    fitness/fitness.component
    hr/hr-users.component
    admin/admin.component
    shared/{forbidden,not-found}
```

All components are standalone. No `NgModule` is used. Feature routes use `loadComponent`/`loadChildren` for lazy loading.

## 4.2 Route protection

`app.routes.ts` nests all authenticated screens under `AppShellComponent` with `authGuard`. Each role group has:

```ts
canActivate: [accessGuard],
data: { roles: ['Pharmacy'], permissions: ['pharmacy.read'] }
```

`accessGuard` allows Admin through any role group because Admin carries every permission. A user without the required role or permission is redirected to `/forbidden`.

`authInterceptor` attaches the JWT and logs out on a 401. `AuthService.restoreSession` restores the session from storage and validates it against `/api/auth/me`.

## 4.3 State and data access

- Angular signals hold session, lists and selected records.
- `ApiService` is the shared typed HTTP facade, grouped into `patients`, `reference`, `clinical`, `pharmacy`, `billing`, `hmo`, `fitness`, `hr`, `admin`.
- Components subscribe with RxJS and use signals for template state.
- No global state library is used; the app is small enough for service signals.

## 4.4 Spartan UI plan

Installed: `@spartan-ng/brain` 1.10.0 and Angular CDK 22.

The styled wrapper layer is local Tailwind components (`btn-primary`, `input`, `card`, `badge`, `table`) because the Helm package (`@spartan-ng/helm`) was unavailable from the npm registry during scaffolding. Spartan Brain is wired for interactive primitives; `BrnButton` is used on the Pharmacy commit control.

Production plan:

| UI need | Spartan primitive | Local wrapper |
|---|---|---|
| Dialogs and confirmations | `BrnDialog` / `BrnAlertDialog` | `card` + overlay |
| Buttons | `BrnButton` | `btn-primary`, `btn-secondary`, `btn-danger` |
| Inputs | Brain input/field primitives | `input`, `label` |
| Selects | Brain select/combobox | native select in scaffold |
| Tables | Brain table primitives or plain table | `table` classes |
| Toast | Brain toast | local notice banner |
| Tabs | Brain tabs | role page sections |

When Helm becomes available, generate wrappers with the Spartan CLI, then replace the local wrappers incrementally without changing feature components.

## 4.5 Representative feature: Pharmacy commit-to-Finance

Path: `/pharmacy/prescriptions`

What it does:

1. Loads `/api/pharmacy/queue` (prescriptions not fully dispensed).
2. Loads `/api/pharmacy/encounters/{encounterId}/prescription`.
3. Shows stock, prescribed/remaining quantity, cash price and active HMO tariff price/coverage.
4. Defaults dispense quantities to the min of remaining and stock.
5. Confirms the action in a dialog.
6. Posts `/api/pharmacy/dispenses`.
7. For outpatients the API creates medication bill lines, confirms matched HMO tariffs and commits the bill to Finance. For inpatients it records ward stock and leaves the charge to Nurse administration.
8. Shows the returned bill status, patient/HMO totals and warnings.

The screen also accepts an encounter ID query parameter, so the Doctor screen can deep-link directly to the prescription.

## 4.6 Representative feature: Finance

Path: `/finance/bills`

- Filters by status.
- Shows patient/HMO split, balance, PA and approval timestamps.
- Routes HMO bills to the HMO Office.
- Records a payment with idempotency key and posts an account-book income entry.
- Approves a bill; HMO bills are blocked until HMO approval and co-pay exist.
- Applies an audited negotiated discount.
- Shows the receivable created on Finance approval.

## 4.7 UI conventions

- Currency: NGN, two decimals.
- Dates: Angular `DatePipe`; API stores UTC and PostgreSQL timestamps.
- Every destructive/irreversible action asks for confirmation or a reason.
- Permission-gated actions are hidden when the permission is absent; the API still enforces the policy.
- Role navigation is generated from permission metadata, but route groups add an explicit role check.

## 4.8 Build and run

```bash
cd frontend/hms-web
npm install
npm start
npm run build
```

The API base URL is in `src/app/core/api.config.ts` and is currently `http://localhost:5080/api`.

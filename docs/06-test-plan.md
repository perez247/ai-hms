# 6. Test plan

## 6.1 Implemented tests

### Unit tests — `backend/tests/Hms.UnitTests`

**Billing calculations**

- HMO/patient allocation rounds without losing kobo.
- Uncovered line is fully patient-payable.
- Line arithmetic (`net = gross - discount = hmo + patient`).
- Bill totals apply only approved adjustments, include payments and compute outstanding balance.
- Bed nights are per overnight with a minimum of one.
- Invalid line splits are rejected.

**State transitions**

- Self-pay Committed → Approved is allowed.
- HMO Committed → Approved is blocked.
- HMO bill requires PA + HMO approval + Finance approval before payable.
- Finance approval alone is insufficient for an HMO bill.
- Paid and Void are terminal.

**CQRS handlers and validators**

- Patient command handler dispatches to its application port.
- Bill approval handler approves and returns the refreshed bill.
- Current-user query returns the role-derived permission set.
- Patient validator accepts a valid request and rejects missing names, future DOB, invalid email and multiple primary HMOs.
- Dispense validator rejects missing lines and non-positive quantities.
- Billing validators reject empty IDs and zero payments.
- HMO PA validator requires exactly one encounter/admission context.
- Validation pipeline blocks invalid requests before the handler and passes valid requests through.

**Authorization catalogue**

- Admin has every permission.
- Pharmacy can approve, negotiate and settle bills on the Bills page but cannot read or post to the account book.
- Bills moved to debt leave the main bills view, appear in the debt view, and settle out once the balance is paid; HMO bills are owed by the HMO.
- HMO Office cannot approve Finance or record payments.
- Finance cannot dispense or issue HMO authorizations.
- Representative permissions exist for every role.

Run:

```bash
dotnet test backend/tests/Hms.UnitTests/Hms.UnitTests.csproj
```

### Integration tests — `backend/tests/Hms.IntegrationTests`

Uses `WebApplicationFactory<Program>` with a PostgreSQL Testcontainer. The database is migrated and seeded for each fixture.

**HMO PA-code path**

1. Pharmacy loads Ada's outpatient prescription.
2. Pharmacy dispenses both medications; the bill moves to Committed.
3. Finance routes the bill to HMO Pending.
4. HMO Office records a PA code.
5. HMO Office approves; the bill is HMO Approved and Finance approval is still absent.
6. Finance records the patient co-pay.
7. Finance approves; an HMO receivable is created for the HMO portion.
8. Assertions verify both approval timestamps and the receivable amount.

**Authorization and validation**

- Pharmacy receives HTTP 403 when calling the Finance approval endpoint.
- A zero-amount payment returns HTTP 400 with the consistent validation payload.

Run:

```bash
dotnet test backend/tests/Hms.IntegrationTests/Hms.IntegrationTests.csproj
```

### Frontend test

The Angular app has a Vitest smoke test for the standalone root component. Run:

```bash
cd frontend/hms-web
npm test -- --watch=false
```

## 6.2 Authorization matrix to add as endpoint tests

For every role, test a representative forbidden and allowed endpoint. The minimum matrix:

| Role | Allowed | Forbidden |
|---|---|---|
| Receptionist | create patient | approve bill |
| Nurse | record vitals | dispense medication |
| Doctor | create encounter | approve payment |
| Pharmacy | commit outpatient bill / approve, negotiate and settle bills | read or post to the account book |
| Finance | approve bill / record payment | dispense / HMO approve |
| HMO Office | issue PA / HMO approve | Finance approve / payment |
| HR Office | create user / export | clinical endpoints |
| Admin | all | none (except business rules) |

## 6.3 Billing edge-case tests

Add:

- Partial payment then final payment transitions Approved → Paid.
- HMO remittance settles the receivable and bill.
- Duplicate payment idempotency key does not double-post.
- Negotiated discount cannot exceed patient portion.
- Post-commit charge is rejected.
- Expired PA blocks HMO approval.
- Missing tariff confirmation blocks HMO approval.
- HMO receivable amount equals HMO payable.
- Bed-day generation is idempotent across repeated discharge requests.
- Discharge blocked when bill is Committed/HmoPending/HmoApproved.

## 6.4 Manual end-to-end checklist

1. Register a new patient with two HMOs; verify two enrollment rows and a registration bill.
2. Renew after expiry; verify new current period and renewal fee.
3. Book an appointment; verify fee and doctor slot uniqueness.
4. Record vitals as Nurse; verify Doctor can view them.
5. Create outpatient encounter and prescription.
6. Dispense partial quantities; verify prescription status and stock movement.
7. Commit to Finance; verify no Pharmacy approval permission.
8. Route HMO bill; verify HMO-only approval.
9. Record co-pay and Finance approval; verify receivable.
10. Create inpatient admission; add multiple inpatient encounter entries.
11. Dispense inpatient medication; verify no bill line until Nurse administration.
12. Record administration; verify medication charge on admission bill.
13. Request discharge; verify bed-day lines and committed bill.
14. Block Doctor discharge until Finance approval.
15. Complete discharge after approval.
16. Verify audit entries for every monetary and clinical transition.

## 6.5 Non-functional tests

- Load test patient search and bill listing with 100k+ rows.
- Verify API authorization with an expired JWT and a disabled user.
- Verify CORS only allows the configured origin.
- Verify migration from a fresh database and from the previous release.
- Verify backup/restore and audit-log immutability.
- Verify file upload size limits and malware scanning.

# 7. Regulatory, clinical-records and go-live checklist

Confirm the following with the clinic, legal counsel and the relevant health-data regulator before production use.

## 7.1 Clinical records

- **Retention period:** confirm the required retention for encounters, investigation reports, surgery notes, discharge summaries and fitness records. Paediatric and mental-health records may require longer retention.
- **Immutability:** clinical notes and reports should not be hard-deleted. This scaffold records audit entries and avoids delete endpoints, but the database permissions should prevent ordinary users from deleting clinical rows.
- **Corrections:** decide whether corrections require an addendum, version history or a formal amendment workflow. The audit log captures before/after values but does not yet provide a patient-facing amendment workflow.
- **E-signatures:** confirm whether reports, discharge summaries and prescriptions require a clinician e-signature.
- **Consent:** capture consent for treatment, HMO data sharing, fitness disclosure and any secondary use. This is not yet a first-class consent table.
- **Access logging:** audit access to records, not only changes. The current audit log covers state changes; read-access logging may be required.

## 7.2 HMO and claims

- **PA validation:** confirm whether the HMO requires prospective, concurrent or retrospective PA for emergencies.
- **PA expiry:** the system blocks HMO approval after PA expiry; confirm the operating rule.
- **Tariff authority:** confirm who may approve a tariff version and whether HMO Office may change coverage unilaterally.
- **Claims submission:** this version records receivables; it does not submit claims to a payer portal or clearing house.
- **Reconciliation:** define the HMO remittance matching and write-off approval process.

## 7.3 Financial records

- Confirm retention for bills, payments, receipts, receivables and the account book.
- Decide whether the account book must integrate with the clinic's accounting package or tax reporting.
- Confirm rounding, currency and tax treatment. This scaffold uses NGN, two decimal places and no tax.
- Confirm who may void a bill after payment and what approval is required.
- Add receipt numbering and patient-facing receipts if required.

## 7.4 Data protection and security

- Encrypt data at rest and in transit; rotate JWT signing keys and database credentials.
- Restrict database and backup access; audit-log rows contain PHI.
- Review CORS, rate limiting, password policy, MFA and session timeout.
- Implement a data-subject request process (access, correction, export, deletion where legally permitted).
- Confirm cross-border data transfer and hosting rules.
- Add malware scanning and content validation for uploaded tariff/report files.
- Add backup, restore and disaster-recovery testing.

## 7.5 Operational controls

- Define who may create Admin accounts and how break-glass access is monitored.
- Remove seeded demo accounts and change every seed password before go-live.
- Define HMO contract renewal ownership and escalation before expiry.
- Define registration-expiry handling at the front desk.
- Train users on the difference between Pharmacy commit, HMO approval and Finance approval.

## 7.6 Known gaps in this scaffold

- No external HMO API or claims clearing-house integration.
- No email/SMS notification channel; expiry alerts are logged only.
- No dedicated consent or read-access audit tables.
- No XLSX export; CSV is provided and opens in spreadsheet software.
- No supplemental-bill flow for charges added after commit.
- No production-grade file storage or malware scanning.
- The simple CSV tariff parser should be replaced with a validated import pipeline.
- `@spartan-ng/helm` was unavailable during scaffolding; the styled layer is local Tailwind and should be migrated to Helm wrappers when available.

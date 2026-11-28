import { TestBed } from '@angular/core/testing';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { PromptService } from '../../core/prompt.service';
import { ToastService } from '../../core/toast.service';
import { Bill, BillLine } from '../../core/models';
import { BillsComponent } from './bills.component';

/** Invoice formatting: compacted groups catalogue lines by item type, detailed lists every line. */
describe('BillsComponent invoice formats', () => {
  function line(description: string, category: string, itemType: string | null,
    quantity: number, netAmount: number): BillLine {
    return {
      id: crypto.randomUUID(), category, itemType, description, quantity, unitPrice: netAmount / quantity,
      grossAmount: netAmount, discountAmount: 0, netAmount, hmoAmount: 0, patientAmount: netAmount,
      isHmoCovered: false, tariffItemId: null, tariffConfirmedAt: null, referralDestination: null,
      encounterId: null, admissionId: null, chargeDate: new Date().toISOString(), createdBy: null,
    };
  }

  const bill: Bill = {
    id: 'bill-1', billNumber: 'BILL-TEST-1', kind: 'Visit', status: 'Committed',
    patientId: 'patient-1', patientNumber: 'PT-1', patientName: 'Test Patient',
    payerType: 'Private', hmoCompanyId: null, hmoCompanyName: null,
    priorAuthorizationId: null, paCode: null, currency: 'NGN',
    subtotal: 20_500, discountTotal: 0, hmoPayableTotal: 0, patientPayableTotal: 20_500,
    paidTotal: 0, balance: 20_500, encounterId: null, admissionId: null,
    appointmentId: null, registrationPeriodId: null, fitnessCertificateId: null,
    createdAt: new Date().toISOString(), committedAt: null, hmoApprovedAt: null,
    financeApprovedAt: null, paidAt: null,
    isDebt: false, debtOwner: null, debtRecordedAt: null, debtReason: null, debtSettledAt: null,
    payments: [], adjustments: [], receivable: null, medicationAdministrations: null,
    lines: [
      line('Amoxicillin', 'Medication', 'Medication', 2, 1_000),
      line('Paracetamol', 'Medication', 'Medication', 3, 1_500),
      line('Complete blood count', 'Investigation', 'Test', 1, 5_000),
      line('Malaria test', 'Investigation', 'Test', 1, 3_000),
      line('Patient registration', 'Registration', null, 1, 10_000),
    ],
  };

  let component: BillsComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [BillsComponent],
      providers: [
        { provide: ApiService, useValue: {} },
        { provide: ToastService, useValue: { success: () => undefined, error: () => undefined } },
        { provide: PromptService, useValue: { open: async () => null } },
        { provide: AuthService, useValue: { hasPermission: () => true } },
      ],
    });
    component = TestBed.createComponent(BillsComponent).componentInstance;
    component.selected.set(bill);
  });

  it('compacts catalogue items by item type and keeps other charges individual', () => {
    component.setPrintFormat('compact');
    const rows = component.invoiceRows();

    expect(rows).toHaveLength(3);
    expect(rows[0]).toEqual({ description: 'Medication', quantity: 5, amount: 2_500 });
    expect(rows[1]).toEqual({ description: 'Tests', quantity: 2, amount: 8_000 });
    expect(rows[2]).toEqual({ description: 'Patient registration', quantity: 1, amount: 10_000 });
  });

  it('lists every line individually in the detailed format', () => {
    component.setPrintFormat('detailed');
    const rows = component.invoiceRows();

    expect(rows).toHaveLength(5);
    expect(rows.map((row) => row.description)).toContain('Amoxicillin');
    expect(rows.map((row) => row.description)).toContain('Malaria test');
  });

  it('prints the same grand total for both formats', () => {
    component.setPrintFormat('compact');
    const compactTotal = component.invoiceRows().reduce((sum, row) => sum + row.amount, 0);
    component.setPrintFormat('detailed');
    const detailedTotal = component.invoiceRows().reduce((sum, row) => sum + row.amount, 0);

    expect(compactTotal).toBe(20_500);
    expect(detailedTotal).toBe(compactTotal);
    expect(component.invoiceTotal()).toBe(20_500);
  });

  it('offers the debt action only for unpaid, non-debt bills', () => {
    expect(component.canMoveToDebt(bill)).toBe(true);
    expect(component.canMoveToDebt({ ...bill, isDebt: true })).toBe(false);
    expect(component.canMoveToDebt({ ...bill, balance: 0 })).toBe(false);
    expect(component.canMoveToDebt({ ...bill, status: 'Paid' })).toBe(false);
  });

  it('names the HMO as the debt owner for HMO bills', () => {
    expect(component.debtOwnerLabel({ ...bill, debtOwner: 'Patient' })).toBe('Test Patient');
    expect(component.debtOwnerLabel({ ...bill, debtOwner: 'Hmo', hmoCompanyName: 'Acme HMO' })).toBe('Acme HMO');
  });
});

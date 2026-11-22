import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { PromptService } from '../../core/prompt.service';
import { ToastService } from '../../core/toast.service';
import { Bill, HmoDirectoryEntry, HmoOption, HmoTariffItem } from '../../core/models';

interface BillLineDraft {
  category: string; description: string; quantity: number | null;
  unitPrice: number | null; tariffCode: string;
}

/** Finance workbench: inspect committed bills, route HMO, negotiate, pay and approve. */
@Component({
  selector: 'app-bills',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './bills.component.html',
})
export class BillsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly prompts = inject(PromptService);
  readonly bills = signal<Bill[]>([]);
  readonly selected = signal<Bill | null>(null);
  readonly status = signal('');
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  readonly paymentAmount = signal(0);
  readonly paymentMethod = signal('Cash');
  readonly paymentReference = signal('');
  readonly notes = signal('');
  readonly hmos = signal<HmoOption[]>([]);
  readonly hmoDirectory = signal<HmoDirectoryEntry[]>([]);
  readonly createLines = signal<BillLineDraft[]>([{ category: 'Consultation', description: '', quantity: 1, unitPrice: null, tariffCode: '' }]);
  readonly createPatientNumber = signal('');
  readonly createHmoId = signal('');
  readonly creating = signal(false);
  readonly createWarnings = signal<string[]>([]);
  readonly tariffItems = signal<HmoTariffItem[]>([]);

  ngOnInit(): void {
    this.load();
    this.api.reference.hmos().subscribe({ next: (hmos) => this.hmos.set(hmos), error: () => undefined });
    this.api.hmo.directory().subscribe({ next: (entries) => this.hmoDirectory.set(entries), error: () => undefined });
  }

  get selectedTariffItems(): HmoTariffItem[] {
    const id = this.createHmoId();
    const entry = this.hmoDirectory().find((item) => item.id === id);
    return entry?.tariffs.find((tariff) => tariff.status === 'Active')?.items ?? [];
  }

  setCreateHmo(id: string): void {
    this.createHmoId.set(id);
    this.tariffItems.set(this.selectedTariffItems);
  }

  addCreateLine(): void {
    this.createLines.update((lines) => [...lines, { category: 'Medication', description: '', quantity: 1, unitPrice: null, tariffCode: '' }]);
  }

  updateCreateLine(index: number, patch: Partial<BillLineDraft>): void {
    this.createLines.update((lines) => lines.map((line, i) => i === index ? { ...line, ...patch } : line));
  }

  applyTariff(index: number, code: string): void {
    const item = this.tariffItems().find((tariff) => tariff.code === code);
    this.updateCreateLine(index, {
      tariffCode: code,
      description: item?.description ?? this.createLines()[index].description,
      unitPrice: item?.unitPrice ?? this.createLines()[index].unitPrice,
    });
  }

  createBill(): void {
    const patientNumber = this.createPatientNumber().trim();
    if (!patientNumber) { this.toast.error('Enter a patient number.'); return; }
    const lines = this.createLines().filter((line) => line.description.trim() && line.quantity && line.unitPrice !== null);
    if (lines.length === 0) { this.toast.error('Add at least one complete bill line.'); return; }
    this.creating.set(true);
    this.api.patients.getByNumber(patientNumber).subscribe({
      next: (details) => this.api.billing.create({
        patientId: details.patient.id,
        hmoCompanyId: this.createHmoId() || null,
        hmoEnrollmentId: null,
        description: 'Manual Finance bill',
        lines: lines.map((line) => ({
          category: line.category, description: line.description.trim(), quantity: Number(line.quantity),
          unitPrice: Number(line.unitPrice), tariffCode: line.tariffCode || null,
        })),
      }).subscribe({
        next: (result) => {
          this.creating.set(false);
          this.createWarnings.set(result.warnings);
          this.toast.success(`Bill ${result.bill.billNumber} created.`);
          this.createLines.set([{ category: 'Consultation', description: '', quantity: 1, unitPrice: null, tariffCode: '' }]);
          this.createHmoId.set('');
          this.tariffItems.set([]);
          this.load();
          this.select(result.bill);
          if (result.warnings.length) this.toast.error(result.warnings.join(' '));
        },
        error: (err) => { this.creating.set(false); this.toast.error(err?.error?.detail ?? 'Could not create the bill.'); },
      }),
      error: (err) => { this.creating.set(false); this.toast.error(err?.error?.detail ?? 'Patient number not found.'); },
    });
  }

  load(): void {
    this.api.billing.search(this.status() || undefined, undefined, 1, 100).subscribe({
      next: (page) => {
        this.bills.set(page.items);
        const current = this.selected();
        if (current) this.api.billing.get(current.id).subscribe({ next: (bill) => this.select(bill) });
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load bills.'),
    });
  }

  select(bill: Bill): void {
    this.selected.set(bill);
    this.paymentAmount.set(bill.balance);
    this.notes.set('');
  }

  inspect(id: string): void {
    this.api.billing.get(id).subscribe({
      next: (bill) => this.select(bill),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load bill.'),
    });
  }

  routeHmo(): void {
    const bill = this.selected();
    if (!bill) return;
    this.execute(this.api.billing.routeHmo(bill.id), 'Bill routed to the HMO Office.');
  }

  recordPayment(): void {
    const bill = this.selected();
    if (!bill) return;
    this.execute(this.api.billing.pay(bill.id, {
      amount: Number(this.paymentAmount()), method: this.paymentMethod(),
      reference: this.paymentReference() || null, idempotencyKey: crypto.randomUUID(), notes: this.notes() || null,
    }), 'Payment recorded in the account book.');
  }

  approve(): void {
    const bill = this.selected();
    if (!bill) return;
    this.execute(this.api.billing.approve(bill.id, this.notes() || null), 'Finance approval recorded.');
  }

  async negotiate(): Promise<void> {
    const bill = this.selected();
    if (!bill) return;
    const values = await this.prompts.open({
      title: 'Negotiated price',
      description: `Record a discount on the patient portion of ${bill.billNumber}.`,
      fields: [
        { key: 'amount', label: 'Discount amount', type: 'number', value: 0 },
        { key: 'reason', label: 'Audit reason', placeholder: 'Reason for the negotiated price' },
      ],
      confirmLabel: 'Record discount',
    });
    if (!values) return;
    const amount = Number(values['amount']);
    const reason = (values['reason'] ?? '').trim();
    if (amount <= 0 || !reason) { this.toast.error('Enter a positive discount and an audit reason.'); return; }
    this.execute(this.api.billing.negotiate(bill.id, amount, reason), 'Negotiated discount recorded with audit trail.');
  }

  canPay(bill: Bill): boolean {
    return !['Draft', 'Void', 'Paid'].includes(bill.status) && bill.balance > 0;
  }

  statusClass(status: string): string {
    switch (status) {
      case 'Paid': return 'badge-green';
      case 'Approved': return 'badge-blue';
      case 'HmoPending': case 'HmoApproved': return 'badge-amber';
      case 'Void': return 'badge-red';
      default: return 'badge-slate';
    }
  }

  // Keeps busy/error state consistent for every mutating bill action.
  private execute(request: Observable<Bill>, message: string): void {
    this.busy.set(true);
    request.subscribe({
      next: (bill) => {
        this.busy.set(false);
        this.select(bill);
        this.notice.set(message);
        this.load();
      },
      error: (err: unknown) => {
        this.busy.set(false);
        this.toast.error((err as { error?: { detail?: string } })?.error?.detail ?? 'Action failed.');
      },
    });
  }
}

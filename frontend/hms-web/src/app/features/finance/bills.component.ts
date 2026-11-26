import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { PromptService } from '../../core/prompt.service';
import { ToastService } from '../../core/toast.service';
import { Bill, BillLine, HmoDirectoryEntry, HmoOption, HmoTariffItem, StockItem } from '../../core/models';

interface BillLineDraft {
  category: string; description: string; quantity: number | null;
  unitPrice: number | null; tariffCode: string;
}

interface InvoiceRow {
  description: string; quantity: number; amount: number;
}

/** Shared Finance/Pharmacy workbench: inspect committed bills, route HMO, negotiate, pay and approve. */
@Component({
  selector: 'app-bills',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './bills.component.html',
})
export class BillsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly prompts = inject(PromptService);
  readonly auth = inject(AuthService);
  readonly bills = signal<Bill[]>([]);
  readonly selected = signal<Bill | null>(null);
  readonly view = signal<'bills' | 'debt'>('bills');
  readonly status = signal('');
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  readonly printOpen = signal(false);
  readonly printFormat = signal<'compact' | 'detailed'>('compact');
  readonly paymentAmount = signal(0);
  readonly paymentMethod = signal('Cash');
  readonly paymentReference = signal('');
  readonly notes = signal('');
  readonly hmos = signal<HmoOption[]>([]);
  readonly hmoDirectory = signal<HmoDirectoryEntry[]>([]);
  readonly createLines = signal<BillLineDraft[]>([{ category: 'Medication', description: '', quantity: 1, unitPrice: null, tariffCode: '' }]);
  readonly createPatientNumber = signal('');
  readonly createHmoId = signal('');
  readonly creating = signal(false);
  readonly createWarnings = signal<string[]>([]);
  readonly tariffItems = signal<HmoTariffItem[]>([]);
  readonly serviceItems = signal<StockItem[]>([]);
  readonly serviceItemId = signal('');
  readonly serviceQuantity = signal(1);

  ngOnInit(): void {
    this.load();
    this.api.reference.hmos().subscribe({ next: (hmos) => this.hmos.set(hmos), error: () => undefined });
    this.api.reference.stockItems().subscribe({ next: (items) => this.serviceItems.set(items) });
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
          this.createLines.set([{ category: 'Medication', description: '', quantity: 1, unitPrice: null, tariffCode: '' }]);
          this.createHmoId.set('');
          this.tariffItems.set([]);
          this.load();
          this.select(result.bill);
          if (result.warnings.length) this.toast.error(result.warnings.join(' '));
        },
        error: (err) => { this.creating.set(false); void err; },
      }),
      error: (err) => { this.creating.set(false); void err; },
    });
  }

  load(): void {
    this.api.billing.search(this.status() || undefined, undefined, 1, 100, this.view() === 'debt').subscribe({
      next: (page) => {
        this.bills.set(page.items);
        const current = this.selected();
        if (current) this.api.billing.get(current.id).subscribe({ next: (bill) => this.select(bill) });
      },
      error: (err) => void err,
    });
  }

  setView(view: 'bills' | 'debt'): void {
    if (this.view() === view) return;
    this.view.set(view);
    this.selected.set(null);
    this.notice.set(null);
    this.load();
  }

  select(bill: Bill): void {
    this.selected.set(bill);
    this.paymentAmount.set(bill.balance);
    this.notes.set('');
  }

  inspect(id: string): void {
    this.api.billing.get(id).subscribe({
      next: (bill) => this.select(bill),
      error: (err) => void err,
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

  /** Moves the unpaid balance off the bills list: the patient owes self-pay debt, the HMO owes HMO debt. */
  async moveToDebt(): Promise<void> {
    const bill = this.selected();
    if (!bill) return;
    const owner = this.debtOwnerLabel(bill);
    const values = await this.prompts.open({
      title: 'Move bill to debt',
      description: `${bill.billNumber} leaves the bills list and is tracked as debt owed by ${owner}.`,
      fields: [{ key: 'reason', label: 'Reason (optional)', placeholder: 'Why is this bill being moved to debt?' }],
      confirmLabel: 'Move to debt',
    });
    if (!values) return;
    const reason = (values['reason'] ?? '').trim();
    this.execute(this.api.billing.moveToDebt(bill.id, reason || null),
      `Bill moved to debt. ${owner} now owes the outstanding balance.`);
  }

  debtOwnerLabel(bill: Bill): string {
    return bill.debtOwner === 'Hmo' ? (bill.hmoCompanyName || 'HMO') : bill.patientName;
  }

  canMoveToDebt(bill: Bill): boolean {
    return this.canManageBills && !bill.isDebt && bill.balance > 0
      && !['Draft', 'Void', 'Paid'].includes(bill.status);
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
        void err;
      },
    });
  }

  addServiceLine(): void {
    const bill = this.selected();
    if (!bill || !this.serviceItemId()) { this.toast.error('Choose a service item first.'); return; }
    this.api.billing.addLine(bill.id, {
      stockItemId: this.serviceItemId(), quantity: Number(this.serviceQuantity()), description: null,
    }).subscribe({
      next: (updated) => { this.selected.set(updated); this.load(); this.toast.success('Service item added to the bill.'); },
    });
  }

  removeLine(lineId: string): void {
    const bill = this.selected();
    if (!bill) return;
    this.api.billing.removeLine(bill.id, lineId).subscribe({
      next: (updated) => { this.selected.set(updated); this.load(); this.toast.success('Line removed from the bill.'); },
    });
  }

  get canManageBills(): boolean { return this.auth.hasPermission('billing.approve'); }
  get canNegotiate(): boolean { return this.auth.hasPermission('billing.negotiate'); }
  get canRecordPayment(): boolean { return this.auth.hasPermission('billing.payment'); }

  openPrint(): void {
    this.printFormat.set('compact');
    this.printOpen.set(true);
  }

  closePrint(): void { this.printOpen.set(false); }

  setPrintFormat(format: 'compact' | 'detailed'): void { this.printFormat.set(format); }

  /** Compacted invoice groups catalogue items by type; detailed lists every line individually. */
  invoiceRows(): InvoiceRow[] {
    const bill = this.selected();
    if (!bill) return [];
    if (this.printFormat() === 'detailed') {
      return bill.lines.map((line) => ({ description: line.description, quantity: line.quantity, amount: line.netAmount }));
    }
    return this.compactRows(bill.lines);
  }

  private compactRows(lines: BillLine[]): InvoiceRow[] {
    const rows: InvoiceRow[] = [];
    const groups = new Map<string, InvoiceRow>();
    for (const line of lines) {
      if (!line.itemType) {
        rows.push({ description: line.description, quantity: line.quantity, amount: line.netAmount });
        continue;
      }
      const existing = groups.get(line.itemType);
      if (existing) {
        existing.quantity += line.quantity;
        existing.amount += line.netAmount;
      } else {
        const row = { description: this.itemTypeLabel(line.itemType), quantity: line.quantity, amount: line.netAmount };
        groups.set(line.itemType, row);
        rows.push(row);
      }
    }
    return rows;
  }

  private itemTypeLabel(type: string): string {
    switch (type) {
      case 'Medication': return 'Medication';
      case 'Test': return 'Tests';
      case 'Procedure': return 'Procedures';
      case 'Service': return 'Services';
      default: return type;
    }
  }

  invoiceTotal(bill: Bill | null = this.selected()): number {
    return bill ? bill.lines.reduce((sum, line) => sum + line.netAmount, 0) : 0;
  }

  printInvoice(): void {
    const bill = this.selected();
    if (!bill) return;
    const rows = this.invoiceRows();
    const total = this.invoiceTotal(bill);
    const currency = bill.currency || 'NGN';
    const money = (amount: number) => new Intl.NumberFormat('en-NG', {
      style: 'currency', currency, minimumFractionDigits: 2,
    }).format(amount);
    const rowsHtml = rows.map((row) => `<tr><td>${this.escapeHtml(row.description)}</td><td class="num">${row.quantity}</td><td class="num">${money(row.amount)}</td></tr>`).join('');
    const windowRef = window.open('', '_blank', 'width=900,height=700');
    if (!windowRef) return;
    windowRef.document.write(`<!doctype html><html><head><title>Invoice ${this.escapeHtml(bill.billNumber)}</title>
      <style>
        body{font-family:system-ui,-apple-system,sans-serif;color:#0f172a;margin:32px}
        h1{font-size:22px;margin:0 0 4px}h2{font-size:13px;color:#64748b;margin:0 0 20px;font-weight:600}
        .meta{display:grid;grid-template-columns:1fr 1fr;gap:6px 24px;margin-bottom:20px;font-size:13px}
        table{width:100%;border-collapse:collapse;font-size:13px}th,td{padding:8px 6px;border-bottom:1px solid #e2e8f0;text-align:left}
        th{background:#f8fafc;text-transform:uppercase;font-size:10px;letter-spacing:.08em;color:#475569}
        .num{text-align:right}.total{margin-top:16px;display:flex;justify-content:flex-end;gap:24px;font-size:16px;font-weight:700}
        .note{margin-top:28px;font-size:11px;color:#64748b}
      </style></head><body>
      <h1>HMS Clinic</h1>
      <h2>${this.printFormat() === 'compact' ? 'Compacted' : 'Detailed'} invoice · ${this.escapeHtml(bill.billNumber)}</h2>
      <div class="meta">
        <span><strong>Patient:</strong> ${this.escapeHtml(bill.patientName)} ${bill.patientNumber ? `(${this.escapeHtml(bill.patientNumber)})` : ''}</span>
        <span><strong>Payer:</strong> ${this.escapeHtml(bill.payerType === 'Hmo' ? bill.hmoCompanyName || 'HMO' : 'Private')}</span>
        <span><strong>Status:</strong> ${this.escapeHtml(bill.status)}</span>
        <span><strong>Date:</strong> ${new Date(bill.createdAt).toLocaleDateString()}</span>
      </div>
      <table><thead><tr><th>Description</th><th class="num">Qty</th><th class="num">Amount</th></tr></thead><tbody>${rowsHtml}</tbody></table>
      <div class="total"><span>Grand total</span><span>${money(total)}</span></div>
      <p class="note">This invoice was generated by HMS Clinic.</p>
      </body></html>`);
    windowRef.document.close();
    windowRef.focus();
    windowRef.print();
  }

  private escapeHtml(value: string): string {
    const replacements: Record<string, string> = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
    return value.replace(/[&<>"']/g, (character) => replacements[character] ?? character);
  }

}

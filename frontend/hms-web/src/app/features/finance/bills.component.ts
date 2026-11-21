import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { Bill } from '../../core/models';

/** Finance workbench: inspect committed bills, route HMO, negotiate, pay and approve. */
@Component({
  selector: 'app-bills',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './bills.component.html',
})
export class BillsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly bills = signal<Bill[]>([]);
  readonly selected = signal<Bill | null>(null);
  readonly status = signal('');
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  readonly paymentAmount = signal(0);
  readonly paymentMethod = signal('Cash');
  readonly paymentReference = signal('');
  readonly notes = signal('');

  ngOnInit(): void { this.load(); }

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

  negotiate(): void {
    const bill = this.selected();
    if (!bill) return;
    const amount = Number(prompt('Negotiated discount amount (patient portion):', '0') ?? 0);
    const reason = prompt('Audit reason for the negotiated price:') ?? '';
    if (amount <= 0 || !reason.trim()) return;
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

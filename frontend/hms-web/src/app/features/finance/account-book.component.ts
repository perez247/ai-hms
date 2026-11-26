import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AccountBookEntry, AccountBookSummary } from '../../core/models';

/** Finance income/expense ledger with expenses and externally sourced credits. */
@Component({
  selector: 'app-account-book',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  template: `
    <div class="mx-auto px-4">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Finance</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Account book</h1>
      <p class="mt-1 text-sm text-slate-500">Income from patient and HMO payments, external credits, and clinic expenses.</p>

      @if (summary(); as totals) {
        <div class="mt-5 grid gap-3 sm:grid-cols-3">
          <div class="rounded-xl border border-emerald-200 bg-emerald-50 p-4"><p class="stat-label text-emerald-700">Total credits</p><p class="mt-1 text-2xl font-black text-emerald-800">{{ totals.totalCredits | currency:'NGN':'symbol-narrow':'1.2-2' }}</p></div>
          <div class="rounded-xl border border-amber-200 bg-amber-50 p-4"><p class="stat-label text-amber-700">Total debits</p><p class="mt-1 text-2xl font-black text-amber-800">{{ totals.totalDebits | currency:'NGN':'symbol-narrow':'1.2-2' }}</p></div>
          <div class="rounded-xl border border-teal-200 bg-teal-50 p-4"><p class="stat-label text-teal-700">Balance</p><p class="mt-1 text-2xl font-black text-teal-900">{{ totals.balance | currency:'NGN':'symbol-narrow':'1.2-2' }}</p></div>
        </div>
      }

      <div class="mt-5 grid gap-5 lg:grid-cols-[360px_1fr]">
        <div class="space-y-5">
          <form class="card h-fit" (ngSubmit)="recordCredit()">
            <div class="card-header"><h2 class="font-bold">Record credit</h2></div>
            <div class="card-body space-y-3">
              <div><label class="label">Amount</label><input class="input" type="number" min="0" [(ngModel)]="creditAmount" name="creditAmount" required /></div>
              <div><label class="label">Source</label><input class="input" [(ngModel)]="creditSource" name="creditSource" placeholder="Grant, donation, float top-up" required /></div>
              <div><label class="label">Date</label><input class="input" type="date" [(ngModel)]="creditDate" name="creditDate" required /></div>
              <div><label class="label">Description</label><input class="input" [(ngModel)]="creditDescription" name="creditDescription" required /></div>
              <div><label class="label">Reference</label><input class="input" [(ngModel)]="creditReference" name="creditReference" placeholder="Receipt / transfer reference" /></div>
              <button class="btn-primary w-full" type="submit">Post credit</button>
            </div>
          </form>

          <form class="card h-fit" (ngSubmit)="addExpense()">
            <div class="card-header"><h2 class="font-bold">Record purchase</h2></div>
            <div class="card-body space-y-3">
              <div><label class="label">Category</label><input class="input" [(ngModel)]="category" name="category" required /></div>
              <div><label class="label">Amount</label><input class="input" type="number" min="0" [(ngModel)]="amount" name="amount" required /></div>
              <div><label class="label">Description</label><input class="input" [(ngModel)]="description" name="description" required /></div>
              <div><label class="label">Reference</label><input class="input" [(ngModel)]="reference" name="reference" placeholder="Invoice / receipt" /></div>
              <button class="btn-primary w-full" type="submit">Post purchase</button>
            </div>
          </form>
        </div>

        <div class="card overflow-hidden">
          <div class="flex items-center justify-between border-b border-slate-100 p-4"><h2 class="font-bold">Ledger</h2><button class="btn-secondary" type="button" (click)="load()">Refresh</button></div>
          <div class="overflow-x-auto"><table class="table">
            <thead><tr><th>Entry</th><th>Type</th><th>Category / source</th><th>Description</th><th>Reference</th><th>Occurred</th><th class="text-right">Amount</th></tr></thead>
            <tbody>
              @for (entry of entries(); track entry.id) {
                <tr><td class="font-mono text-xs">{{ entry.entryNumber }}</td><td><span [class]="entry.direction === 'Credit' ? 'badge-green' : 'badge-amber'">{{ entry.direction }}</span><p class="text-[10px] text-slate-400">{{ entry.entryType }}</p></td><td>{{ entry.source || entry.category }}</td><td>{{ entry.description }}</td><td>{{ entry.reference || '—' }}</td><td>{{ entry.occurredAt | date:'medium' }}</td><td class="text-right font-semibold">{{ entry.amount | currency:'NGN':'symbol-narrow':'1.2-2' }}</td></tr>
              }
              @if (entries().length === 0) { <tr><td colspan="7" class="py-10 text-center text-slate-500">No ledger entries.</td></tr> }
            </tbody>
          </table></div>
        </div>
      </div>
    </div>
  `,
})
export class AccountBookComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly entries = signal<AccountBookEntry[]>([]);
  readonly summary = signal<AccountBookSummary | null>(null);
  category = 'Utilities';
  amount = 0;
  description = '';
  reference = '';
  creditAmount = 0;
  creditSource = '';
  creditDate = new Date().toISOString().slice(0, 10);
  creditDescription = '';
  creditReference = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.billing.accountBook(null, null, null).subscribe({
      next: (page) => this.entries.set(page.items),
      error: (err) => void err,
    });
    this.api.billing.accountBookSummary().subscribe({
      next: (summary) => this.summary.set(summary),
      error: (err) => void err,
    });
  }

  addExpense(): void {
    if (!this.category.trim() || this.amount <= 0 || !this.description.trim()) return;
    this.api.billing.recordPurchase({ entryType: 'Expense', category: this.category, amount: Number(this.amount), description: this.description, occurredAt: null, reference: this.reference || null }).subscribe({
      next: () => { this.amount = 0; this.description = ''; this.reference = ''; this.toast.success('Purchase posted.'); this.load(); },
      error: (err) => void err,
    });
  }

  recordCredit(): void {
    if (this.creditAmount <= 0 || !this.creditSource.trim() || !this.creditDescription.trim()) {
      this.toast.error('Enter a positive amount, source and description.');
      return;
    }
    this.api.billing.recordCredit({
      amount: Number(this.creditAmount),
      source: this.creditSource.trim(),
      occurredAt: this.creditDate ? new Date(`${this.creditDate}T00:00:00`).toISOString() : null,
      description: this.creditDescription.trim(),
      reference: this.creditReference.trim() || null,
    }).subscribe({
      next: () => {
        this.creditAmount = 0;
        this.creditSource = '';
        this.creditDescription = '';
        this.creditReference = '';
        this.creditDate = new Date().toISOString().slice(0, 10);
        this.toast.success('Credit posted to the account book.');
        this.load();
      },
      error: (err) => void err,
    });
  }
}

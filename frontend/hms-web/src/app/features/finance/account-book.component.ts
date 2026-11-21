import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AccountBookEntry } from '../../core/models';

/** Finance income/expense ledger with expense posting. */
@Component({
  selector: 'app-account-book',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  template: `
    <div class="mx-auto max-w-6xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Finance</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Account book</h1>
      <p class="mt-1 text-sm text-slate-500">Income from patient and HMO payments, plus clinic expenses.</p>
      <div class="mt-5 grid gap-5 lg:grid-cols-[360px_1fr]">
        <form class="card h-fit" (ngSubmit)="addExpense()">
          <div class="card-header"><h2 class="font-bold">Record expense</h2></div>
          <div class="card-body space-y-3">
            <div><label class="label">Category</label><input class="input" [(ngModel)]="category" name="category" required /></div>
            <div><label class="label">Amount</label><input class="input" type="number" [(ngModel)]="amount" name="amount" required /></div>
            <div><label class="label">Description</label><input class="input" [(ngModel)]="description" name="description" required /></div>
            <button class="btn-primary w-full" type="submit">Post expense</button>
          </div>
        </form>
        <div class="card overflow-hidden">
          <div class="flex items-center justify-between border-b border-slate-100 p-4"><h2 class="font-bold">Ledger</h2><button class="btn-secondary" type="button" (click)="load()">Refresh</button></div>
          <div class="overflow-x-auto"><table class="table">
            <thead><tr><th>Entry</th><th>Type</th><th>Category</th><th>Description</th><th>Occurred</th><th class="text-right">Amount</th></tr></thead>
            <tbody>
              @for (entry of entries(); track entry.id) {
                <tr><td class="font-mono text-xs">{{ entry.entryNumber }}</td><td><span [class]="entry.entryType === 'Income' ? 'badge-green' : 'badge-amber'">{{ entry.entryType }}</span></td><td>{{ entry.category }}</td><td>{{ entry.description }}</td><td>{{ entry.occurredAt | date:'medium' }}</td><td class="text-right font-semibold">{{ entry.amount | currency:'NGN':'symbol-narrow':'1.2-2' }}</td></tr>
              }
              @if (entries().length === 0) { <tr><td colspan="6" class="py-10 text-center text-slate-500">No ledger entries.</td></tr> }
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
  category = 'Utilities';
  amount = 0;
  description = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.billing.accountBook(null, null, null).subscribe({
      next: (page) => this.entries.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load account book.'),
    });
  }

  addExpense(): void {
    if (!this.category.trim() || this.amount <= 0 || !this.description.trim()) return;
    this.api.billing.addExpense({ entryType: 'Expense', category: this.category, amount: Number(this.amount), description: this.description, occurredAt: null }).subscribe({
      next: () => { this.amount = 0; this.description = ''; this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not post expense.'),
    });
  }
}

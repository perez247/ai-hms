import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { HmoDirectoryEntry } from '../../core/models';

interface TariffDraft { code: string; description: string; unitPrice: number | null; coveragePercent: number; }

/** HMO partner directory with self-service registration and negotiated medication tariffs. */
@Component({
  selector: 'app-hmo-directory',
  imports: [RouterLink, FormsModule],
  template: `
    <div class="mx-auto px-4">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">HMO partners</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">HMO profiles</h1>
      <p class="mt-1 text-sm text-slate-500">Register HMOs, maintain negotiated medication tariffs and reach bulk-fitness settlement.</p>

      @if (canRegister) {
        <form class="card mt-5" (ngSubmit)="register()">
          <div class="card-header"><h2 class="font-bold">Register an HMO</h2><p class="text-xs text-slate-500">Optionally include negotiated medication prices in the first tariff.</p></div>
          <div class="card-body grid gap-3 md:grid-cols-3">
            <div><label class="label">Code</label><input class="input" [(ngModel)]="code" name="code" placeholder="e.g. HMO-NEW" /></div>
            <div><label class="label">Name</label><input class="input" [(ngModel)]="name" name="name" /></div>
            <div><label class="label">Contact person</label><input class="input" [(ngModel)]="contactPerson" name="contactPerson" /></div>
            <div><label class="label">Email</label><input class="input" [(ngModel)]="email" name="email" /></div>
            <div><label class="label">Phone</label><input class="input" [(ngModel)]="phone" name="phone" /></div>
            <div><label class="label">Address</label><input class="input" [(ngModel)]="address" name="address" /></div>
            <div class="md:col-span-3"><label class="label">Contract terms</label><input class="input" [(ngModel)]="contractTerms" name="contractTerms" /></div>
            <div class="md:col-span-3 border-t border-slate-100 pt-3">
              <div class="flex items-center justify-between"><p class="stat-label">Negotiated medication prices (optional)</p><button class="btn-secondary" type="button" (click)="addTariffRow()">+ Add medication</button></div>
              <div class="mt-3 space-y-2">
                @for (row of tariffRows(); track $index) {
                  <div class="grid grid-cols-[1fr_1.5fr_1fr_1fr_auto] items-center gap-2">
                    <input class="input" [(ngModel)]="row.code" [name]="'tariffCode' + $index" placeholder="Tariff code" />
                    <input class="input" [(ngModel)]="row.description" [name]="'tariffDescription' + $index" placeholder="Medication" />
                    <input class="input" type="number" [(ngModel)]="row.unitPrice" [name]="'tariffPrice' + $index" placeholder="Unit price" />
                    <input class="input" type="number" [(ngModel)]="row.coveragePercent" [name]="'tariffCoverage' + $index" placeholder="Coverage %" />
                    <button class="text-xs font-bold text-rose-600" type="button" (click)="removeTariffRow($index)">Remove</button>
                  </div>
                }
              </div>
            </div>
            <div class="md:col-span-3 flex justify-end"><button class="btn-primary" type="submit" [disabled]="saving()">{{ saving() ? 'Registering…' : 'Register HMO' }}</button></div>
          </div>
        </form>
      }

      <div class="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        @for (hmo of hmos(); track hmo.id) {
          <a [routerLink]="['/hmos', hmo.id]" class="card p-5 transition hover:border-teal-300 hover:bg-teal-50/50">
            <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ hmo.code }}</p>
            <p class="mt-2 text-lg font-bold text-slate-950">{{ hmo.name }}</p>
            <p class="mt-1 text-xs text-slate-500">{{ hmo.tariffs.length }} tariff version(s)</p>
            <p class="mt-2 text-xs font-semibold text-teal-700">Open HMO profile →</p>
          </a>
        }
        @if (hmos().length === 0) { <div class="card p-8 text-center text-slate-500">No HMOs registered yet.</div> }
      </div>
    </div>
  `,
})
export class HmoDirectoryComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly hmos = signal<HmoDirectoryEntry[]>([]);
  readonly tariffRows = signal<TariffDraft[]>([]);
  readonly saving = signal(false);

  code = '';
  name = '';
  contactPerson = '';
  email = '';
  phone = '';
  address = '';
  contractTerms = '';

  get canRegister(): boolean { return this.auth.hasPermission('hmo.company.manage'); }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.hmo.directory().subscribe({
      next: (entries) => this.hmos.set(entries),
      error: (err) => void err,
    });
  }

  addTariffRow(): void {
    this.tariffRows.update((rows) => [...rows, { code: '', description: '', unitPrice: null, coveragePercent: 100 }]);
  }

  removeTariffRow(index: number): void {
    this.tariffRows.update((rows) => rows.filter((_, i) => i !== index));
  }

  register(): void {
    if (!this.code.trim() || !this.name.trim()) { this.toast.error('HMO code and name are required.'); return; }
    const rows = this.tariffRows().filter((row) => row.code.trim() && row.unitPrice !== null);
    if (this.tariffRows().length && rows.length === 0) { this.toast.error('Enter a code and unit price for each medication row.'); return; }
    this.saving.set(true);
    this.api.hmo.register({
      code: this.code.trim(), name: this.name.trim(), contactPerson: this.contactPerson || null,
      email: this.email || null, phone: this.phone || null, address: this.address || null,
      contractTerms: this.contractTerms || null,
      initialTariff: rows.length ? {
        name: 'Standard medication tariff', effectiveFrom: new Date().toISOString().slice(0, 10), effectiveTo: null,
        items: rows.map((row) => ({
          code: row.code.trim(), description: row.description.trim() || row.code.trim(), category: 'Medication',
          unit: 'unit', unitPrice: Number(row.unitPrice), coveragePercent: Number(row.coveragePercent),
          requiresPriorAuthorization: false,
        })),
      } : null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('HMO registered.');
        this.code = ''; this.name = ''; this.contactPerson = ''; this.email = ''; this.phone = ''; this.address = ''; this.contractTerms = '';
        this.tariffRows.set([]);
        this.load();
      },
      error: (err) => { this.saving.set(false); void err; },
    });
  }
}

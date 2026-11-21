import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { Fee, HmoOption } from '../../core/models';

interface HmoCompanyRow { id: string; code: string; name: string; contactPerson: string | null; email: string | null; phone: string | null; address: string | null; isActive: boolean; }

/** Admin fee and HMO partner configuration. */
@Component({
  selector: 'app-admin',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  template: `
    <div class="mx-auto max-w-6xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Administration</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Configuration</h1>
      <p class="mt-1 text-sm text-slate-500">Fees, HMO partners, contracts and tariff operations. Admin has all role permissions.</p>
      @if (notice()) { <div class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{{ notice() }}</div> }

      <div class="mt-5 grid gap-5 lg:grid-cols-2">
        <div class="card overflow-hidden">
          <div class="card-header"><h2 class="font-bold">Fee schedule</h2><p class="text-xs text-slate-500">Flat fees; changes create a new effective record.</p></div>
          <div class="overflow-x-auto"><table class="table"><thead><tr><th>Code</th><th>Amount</th><th>Effective</th><th>Active</th></tr></thead><tbody>
            @for (fee of fees(); track fee.id) {<tr><td class="font-mono text-xs">{{ fee.code }}</td><td>{{ fee.amount | currency:'NGN':'symbol-narrow':'1.2-2' }}</td><td>{{ fee.effectiveFrom | date:'mediumDate' }}</td><td><span [class]="fee.isActive ? 'badge-green' : 'badge-slate'">{{ fee.isActive }}</span></td></tr>}
          </tbody></table></div>
          <form class="grid gap-3 border-t border-slate-100 p-4 sm:grid-cols-2" (ngSubmit)="createFee()">
            <div><label class="label">Fee code</label><select class="input" [(ngModel)]="feeCode" name="feeCode">@for (code of feeCodes; track code) {<option [value]="code">{{ code }}</option>}</select></div>
            <div><label class="label">Amount</label><input class="input" type="number" [(ngModel)]="feeAmount" name="feeAmount" /></div>
            <div><label class="label">Effective from</label><input class="input" type="date" [(ngModel)]="feeEffective" name="feeEffective" /></div>
            <button class="btn-primary self-end" type="submit">Add fee</button>
          </form>
        </div>

        <div class="card overflow-hidden">
          <div class="card-header"><h2 class="font-bold">HMO partners</h2><p class="text-xs text-slate-500">Contracts default to five years when created through the API.</p></div>
          <div class="overflow-x-auto"><table class="table"><thead><tr><th>HMO</th><th>Contact</th><th>Status</th></tr></thead><tbody>
            @for (hmo of hmos(); track hmo.id) {<tr><td><p class="font-semibold">{{ hmo.name }}</p><p class="text-xs text-slate-500">{{ hmo.code }}</p></td><td><p>{{ hmo.contactPerson || '—' }}</p><p class="text-xs text-slate-500">{{ hmo.email || '' }}</p></td><td><span [class]="hmo.isActive ? 'badge-green' : 'badge-slate'">{{ hmo.isActive ? 'Active' : 'Inactive' }}</span></td></tr>}
          </tbody></table></div>
          <form class="grid gap-3 border-t border-slate-100 p-4 sm:grid-cols-2" (ngSubmit)="createHmo()">
            <div><label class="label">Code</label><input class="input" [(ngModel)]="hmoCode" name="hmoCode" /></div>
            <div><label class="label">Name</label><input class="input" [(ngModel)]="hmoName" name="hmoName" /></div>
            <div><label class="label">Contact person</label><input class="input" [(ngModel)]="hmoContact" name="hmoContact" /></div>
            <div><label class="label">Email</label><input class="input" [(ngModel)]="hmoEmail" name="hmoEmail" /></div>
            <button class="btn-primary self-end" type="submit">Register HMO</button>
          </form>
        </div>
      </div>
    </div>
  `,
})
export class AdminComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly fees = signal<Fee[]>([]);
  readonly hmos = signal<HmoCompanyRow[]>([]);
  readonly notice = signal<string | null>(null);
  readonly feeCodes = ['RegistrationNew', 'RegistrationRenewal', 'Appointment', 'Emergency', 'InpatientConsultation', 'BedDayGeneral', 'BedDayPrivate', 'FitnessCertificate'];
  feeCode = 'Appointment';
  feeAmount = 0;
  feeEffective = new Date().toISOString().slice(0, 10);
  hmoCode = '';
  hmoName = '';
  hmoContact = '';
  hmoEmail = '';

  ngOnInit(): void {
    this.api.admin.fees().subscribe({ next: (fees) => this.fees.set(fees), error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load fees.') });
    this.api.admin.hmos().subscribe({ next: (hmos) => this.hmos.set(hmos as HmoCompanyRow[]) });
  }

  createFee(): void {
    if (!this.feeCode || this.feeAmount < 0) return;
    this.api.admin.createFee({ code: this.feeCode, amount: Number(this.feeAmount), currency: 'NGN', effectiveFrom: this.feeEffective, effectiveTo: null }).subscribe({
      next: () => { this.notice.set('Fee added.'); this.api.admin.fees().subscribe({ next: (fees) => this.fees.set(fees) }); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not add fee.'),
    });
  }

  createHmo(): void {
    if (!this.hmoCode.trim() || !this.hmoName.trim()) return;
    this.api.admin.createHmo({ code: this.hmoCode, name: this.hmoName, contactPerson: this.hmoContact || null, email: this.hmoEmail || null, phone: null, address: null, contractTerms: null, metadataJson: null }).subscribe({
      next: () => { this.notice.set('HMO partner registered.'); this.api.admin.hmos().subscribe({ next: (hmos) => this.hmos.set(hmos as HmoCompanyRow[]) }); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not register HMO.'),
    });
  }
}

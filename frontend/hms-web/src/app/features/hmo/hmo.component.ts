import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { Bill, HmoEnrollmentOption, PriorAuthorization } from '../../core/models';

/** HMO Office workbench: issue PA, adjust line coverage and HMO-approve only. */
@Component({
  selector: 'app-hmo',
  imports: [CurrencyPipe, DatePipe, FormsModule],
  templateUrl: './hmo.component.html',
})
export class HmoComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly pending = signal<Bill[]>([]);
  readonly authorizations = signal<PriorAuthorization[]>([]);
  readonly selected = signal<Bill | null>(null);
  readonly enrollments = signal<HmoEnrollmentOption[]>([]);
  readonly notice = signal<string | null>(null);
  readonly busy = signal(false);
  readonly coverage = signal<Record<string, { covered: boolean; hmoAmount: number; referral: string }>>({});
  paCode = '';
  expiresAt = '';
  approvedAmount = 0;
  notes = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.billing.search('HmoPending', undefined, 1, 100).subscribe({
      next: (page) => this.pending.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load HMO-pending bills.'),
    });
    this.api.hmo.authorizations().subscribe({
      next: (page) => this.authorizations.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load authorizations.'),
    });
  }

  select(bill: Bill): void {
    this.selected.set(bill);
    this.paCode = bill.paCode ?? '';
    this.expiresAt = '';
    this.approvedAmount = bill.hmoPayableTotal;
    this.notes = '';
    const coverage: Record<string, { covered: boolean; hmoAmount: number; referral: string }> = {};
    for (const line of bill.lines) coverage[line.id] = { covered: line.isHmoCovered, hmoAmount: line.hmoAmount, referral: line.referralDestination ?? '' };
    this.coverage.set(coverage);
    if (bill.patientId) {
      this.api.reference.enrollments(bill.patientId).subscribe({ next: (options) => this.enrollments.set(options) });
    }
  }

  /** Uses the patient's primary active enrollment; the bill supplies encounter/admission context. */
  savePa(): void {
    const bill = this.selected();
    if (!bill || !this.paCode.trim() || !bill.hmoCompanyId || !bill.patientId) return;
    const enrollment = this.enrollments().find((x) => x.isPrimary) ?? this.enrollments()[0];
    if (!enrollment) { this.toast.error('No active HMO enrollment found for this patient.'); return; }
    this.busy.set(true);
    this.api.hmo.saveAuthorization({
      hmoCompanyId: bill.hmoCompanyId, hmoEnrollmentId: enrollment.id,
      encounterId: bill.encounterId, admissionId: bill.admissionId,
      paCode: this.paCode.trim(), expiresAt: this.expiresAt ? new Date(this.expiresAt).toISOString() : null,
      approvedAmount: Number(this.approvedAmount) || null, notes: this.notes || null,
    }).subscribe({
      next: () => { this.busy.set(false); this.notice.set('PA code recorded and linked to the bill.'); this.reloadBill(); this.load(); },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not save PA.'); },
    });
  }

  /** Persists per-line HMO/patient split and referral destinations before approval. */
  saveCoverage(): void {
    const bill = this.selected();
    if (!bill) return;
    const lines = Object.entries(this.coverage()).map(([billLineId, value]) => ({ billLineId, isCovered: value.covered, hmoAmount: Number(value.hmoAmount) || 0, referralDestination: value.referral || null }));
    this.busy.set(true);
    this.api.hmo.updateCoverage(bill.id, { lines }).subscribe({
      next: (updated) => { this.busy.set(false); this.notice.set('Coverage allocations updated.'); this.select(updated); },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not update coverage.'); },
    });
  }

  approve(): void {
    const bill = this.selected();
    if (!bill) return;
    this.busy.set(true);
    this.api.hmo.approve(bill.id).subscribe({
      next: (updated) => { this.busy.set(false); this.notice.set('HMO approval recorded. Finance final approval is still required.'); this.select(updated); this.load(); },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'HMO approval failed.'); },
    });
  }

  setCoverage(lineId: string, field: 'covered' | 'hmoAmount' | 'referral', value: unknown): void {
    this.coverage.update((current) => ({ ...current, [lineId]: { ...current[lineId], [field]: value } }));
  }

  private reloadBill(): void {
    const bill = this.selected();
    if (!bill) return;
    this.api.billing.get(bill.id).subscribe({ next: (updated) => this.select(updated) });
  }
}

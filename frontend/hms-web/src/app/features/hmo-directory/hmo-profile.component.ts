import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { FitnessCertificate, HmoDirectoryEntry, HmoOption } from '../../core/models';

/**
 * HMO profile. Fitness stays unloaded until its accordion opens. Finance can bulk-pay
 * approved HMO records here; individual HMO payment is intentionally blocked by the API.
 */
@Component({
  selector: 'app-hmo-profile',
  imports: [DatePipe, RouterLink, FormsModule],
  templateUrl: './hmo-profile.component.html',
})
export class HmoProfileComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly hmo = signal<HmoOption | null>(null);
  readonly directoryEntry = signal<HmoDirectoryEntry | null>(null);
  readonly fitness = signal<FitnessCertificate[] | null>(null);
  readonly selected = signal<Set<string>>(new Set());
  readonly loading = signal(false);
  readonly tariffCode = signal('');
  readonly tariffDescription = signal('');
  readonly tariffUnitPrice = signal<number | null>(null);
  readonly tariffCoverage = signal(100);
  readonly savingTariff = signal(false);

  get hmoId(): string { return this.route.snapshot.paramMap.get('id') ?? ''; }

  ngOnInit(): void {
    this.api.reference.hmos().subscribe({
      next: (hmos) => this.hmo.set(hmos.find((hmo) => hmo.id === this.hmoId) ?? null),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load the HMO.'),
    });
    this.loadTariffs();
  }

  loadTariffs(): void {
    this.api.hmo.directory().subscribe({
      next: (entries) => this.directoryEntry.set(entries.find((entry) => entry.id === this.hmoId) ?? null),
      error: () => this.directoryEntry.set(null),
    });
  }

  get canManageTariffs(): boolean { return this.auth.hasPermission('hmo.tariff.manage'); }

  saveTariff(): void {
    const code = this.tariffCode().trim();
    const unitPrice = this.tariffUnitPrice();
    if (!code || unitPrice === null) { this.toast.error('Enter a medication code and negotiated unit price.'); return; }
    this.savingTariff.set(true);
    this.api.hmo.saveTariff(this.hmoId, {
      name: 'Negotiated medication tariff', effectiveFrom: new Date().toISOString().slice(0, 10), effectiveTo: null,
      items: [{
        code, description: this.tariffDescription().trim() || code, category: 'Medication',
        unit: 'unit', unitPrice: Number(unitPrice), coveragePercent: Number(this.tariffCoverage()),
        requiresPriorAuthorization: false,
      }],
    }).subscribe({
      next: () => {
        this.savingTariff.set(false);
        this.toast.success('Tariff saved. It is now the active tariff for this HMO.');
        this.tariffCode.set(''); this.tariffDescription.set(''); this.tariffUnitPrice.set(null); this.tariffCoverage.set(100);
        this.loadTariffs();
      },
      error: (err) => { this.savingTariff.set(false); this.toast.error(err?.error?.detail ?? 'Could not save the tariff.'); },
    });
  }

  loadFitness(event: Event): void {
    if ((event.target as HTMLDetailsElement).open && this.fitness() === null) this.fetchFitness();
  }

  private fetchFitness(): void {
    this.loading.set(true);
    this.api.fitness.search(null, null, this.hmoId, null).subscribe({
      next: (page) => {
        this.fitness.set(page.items.filter((item) => item.status === 'Approved'));
        this.loading.set(false);
      },
      error: (err) => { this.loading.set(false); this.toast.error(err?.error?.detail ?? 'Could not load HMO fitness records.'); },
    });
  }

  toggle(id: string, checked: boolean): void {
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) next.add(id); else next.delete(id);
      return next;
    });
  }

  markPaid(paid: boolean): void {
    const ids = [...this.selected()];
    if (ids.length === 0) { this.toast.error('Select at least one fitness record.'); return; }
    this.api.fitness.batchPayment(this.hmoId, ids, paid).subscribe({
      next: () => { this.toast.success(paid ? 'Selected HMO fitness records marked paid.' : 'Selected records marked unpaid.'); this.selected.set(new Set()); this.fetchFitness(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not update HMO fitness payments.'),
    });
  }

  statusClass(status: string): string {
    if (status === 'Approved' || status === 'Paid') return 'badge-green';
    if (status === 'Rejected') return 'badge-red';
    if (status === 'AwaitingNurse' || status === 'AwaitingDoctor' || status === 'Unpaid') return 'badge-amber';
    return 'badge-slate';
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { FitnessCertificate, HmoOption } from '../../core/models';

/**
 * HMO profile. Fitness stays unloaded until its accordion opens. Finance can bulk-pay
 * approved HMO records here; individual HMO payment is intentionally blocked by the API.
 */
@Component({
  selector: 'app-hmo-profile',
  imports: [DatePipe, RouterLink],
  templateUrl: './hmo-profile.component.html',
})
export class HmoProfileComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly hmo = signal<HmoOption | null>(null);
  readonly fitness = signal<FitnessCertificate[] | null>(null);
  readonly selected = signal<Set<string>>(new Set());
  readonly loading = signal(false);

  get hmoId(): string { return this.route.snapshot.paramMap.get('id') ?? ''; }

  ngOnInit(): void {
    this.api.reference.hmos().subscribe({
      next: (hmos) => this.hmo.set(hmos.find((hmo) => hmo.id === this.hmoId) ?? null),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load the HMO.'),
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

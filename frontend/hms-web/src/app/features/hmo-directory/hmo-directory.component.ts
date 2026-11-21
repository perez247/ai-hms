import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { HmoOption } from '../../core/models';

/** Finance/HMO Office directory used to reach the HMO profile and bulk fitness settlement. */
@Component({
  selector: 'app-hmo-directory',
  imports: [RouterLink],
  template: `
    <div class="mx-auto max-w-5xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">HMO partners</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">HMO profiles</h1>
      <p class="mt-1 text-sm text-slate-500">Open an HMO to view its Fitness accordion and process bulk payments.</p>
      <div class="mt-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        @for (hmo of hmos(); track hmo.id) {
          <a [routerLink]="['/hmos', hmo.id]" class="card p-5 transition hover:border-teal-300 hover:bg-teal-50/50">
            <p class="text-xs font-bold uppercase tracking-wide text-slate-400">{{ hmo.code }}</p>
            <p class="mt-2 text-lg font-bold text-slate-950">{{ hmo.name }}</p>
            <p class="mt-2 text-xs font-semibold text-teal-700">Open HMO profile →</p>
          </a>
        }
        @if (hmos().length === 0) { <div class="card p-8 text-center text-slate-500">No active HMOs.</div> }
      </div>
    </div>
  `,
})
export class HmoDirectoryComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly hmos = signal<HmoOption[]>([]);

  ngOnInit(): void {
    this.api.reference.hmos().subscribe({
      next: (hmos) => this.hmos.set(hmos),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load HMO partners.'),
    });
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { StaffProfile } from '../../core/models';

/** Own profile for all staff; HR/Admin can open any staff member and see employment details. */
@Component({
  selector: 'app-staff-profile',
  imports: [DatePipe, CurrencyPipe],
  template: `
    <div class="mx-auto max-w-4xl">
      @if (profile(); as staff) {
        <div class="flex items-start justify-between gap-4">
          <div><p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Staff profile</p><h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">{{ staff.fullName }}</h1><p class="mt-1 text-sm text-slate-500">{{ staff.role.replace('HmoOffice', 'HMO Office').replace('HrOffice', 'HR Office') }}</p></div>
          <span [class]="staff.isActive ? 'badge-green' : 'badge-red'">{{ staff.isActive ? 'Active' : 'Disabled' }}</span>
        </div>
        <div class="mt-5 grid gap-5 md:grid-cols-2">
          <div class="card p-5"><p class="stat-label">Basic information</p><div class="mt-3 space-y-2 text-sm text-slate-700"><p><span class="font-semibold">Email:</span> {{ staff.email }}</p><p><span class="font-semibold">Phone:</span> {{ staff.phone || '—' }}</p><p><span class="font-semibold">Role:</span> {{ staff.role }}</p></div></div>
          @if (staff.canViewEmployment) {
            <div class="card p-5"><p class="stat-label">Employment information</p><div class="mt-3 space-y-2 text-sm text-slate-700"><p><span class="font-semibold">Employee number:</span> {{ staff.employeeNumber || '—' }}</p><p><span class="font-semibold">Hired on:</span> {{ staff.hiredOn ? (staff.hiredOn | date:'mediumDate') : '—' }}</p><p><span class="font-semibold">Salary:</span> {{ staff.salaryAmount !== null ? (staff.salaryAmount | currency:'NGN':'symbol-narrow':'1.2-2') : '—' }}</p><p><span class="font-semibold">Bank:</span> {{ staff.bankName || '—' }}</p><p><span class="font-semibold">Account name:</span> {{ staff.bankAccountName || '—' }}</p><p><span class="font-semibold">Account number:</span> {{ staff.bankAccountNumber || '—' }}</p></div></div>
          }
        </div>
      } @else {
        <div class="card p-10 text-center text-slate-500">Loading staff profile…</div>
      }
    </div>
  `,
})
export class StaffProfileComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly profile = signal<StaffProfile | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const request = id ? this.api.staff.get(id) : this.api.staff.me();
    request.subscribe({
      next: (staff) => this.profile.set(staff),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load the staff profile.'),
    });
  }
}

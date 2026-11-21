import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { StaffRole } from '../../core/models';

/** Role-aware landing page exposing only links the current permissions allow. */
@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <div class="mx-auto max-w-6xl">
      <div class="overflow-hidden rounded-3xl bg-slate-950 p-8 text-white shadow-xl sm:p-12">
        <p class="text-xs font-bold uppercase tracking-[0.22em] text-teal-300">Clinic operations</p>
        <h1 class="mt-4 max-w-2xl text-4xl font-black tracking-tight sm:text-5xl">
          Good day, {{ auth.user()?.fullName }}.
        </h1>
        <p class="mt-4 max-w-2xl text-slate-300">
          You are signed in as <span class="font-semibold text-white">{{ auth.user()?.roleName }}</span>.
          Your workspace only exposes the actions allowed by your role.
        </p>
        <div class="mt-8 flex flex-wrap gap-3">
          @for (link of links(); track link.path) {
            <a [routerLink]="link.path" class="rounded-xl bg-white px-5 py-3 text-sm font-bold text-slate-900 hover:bg-teal-50">
              {{ link.label }}
            </a>
          }
        </div>
      </div>

      <div class="mt-6 grid gap-5 md:grid-cols-3">
        <div class="stat">
          <p class="stat-label">Current role</p>
          <p class="stat-value">{{ auth.user()?.roleName }}</p>
          <p class="mt-2 text-sm text-slate-500">Permissions are delivered in the signed JWT and checked on every API call.</p>
        </div>
        <div class="stat">
          <p class="stat-label">Permissions granted</p>
          <p class="stat-value">{{ auth.user()?.permissions?.length ?? 0 }}</p>
          <p class="mt-2 text-sm text-slate-500">Admin receives the union of all role permissions; other roles are restricted.</p>
        </div>
        <div class="stat">
          <p class="stat-label">HMO payment gate</p>
          <p class="stat-value">2 approvals</p>
          <p class="mt-2 text-sm text-slate-500">HMO Office approval and Finance approval are both required before an HMO bill is payable.</p>
        </div>
      </div>
    </div>
  `,
})
export class HomeComponent {
  readonly auth = inject(AuthService);

  readonly links = computed(() => {
    const candidates: Array<{ label: string; path: string; permission: string; roles: StaffRole[] }> = [
      { label: 'Patients', path: '/reception/patients', permission: 'patients.read', roles: ['Receptionist'] },
      { label: 'Pharmacy queue', path: '/pharmacy/prescriptions', permission: 'pharmacy.read', roles: ['Pharmacy'] },
      { label: 'Finance bills', path: '/finance/bills', permission: 'billing.read', roles: ['Finance'] },
      { label: 'HMO desk', path: '/hmo/authorizations', permission: 'hmo.read', roles: ['HmoOffice'] },
      { label: 'HR users', path: '/hr/users', permission: 'hr.users.read', roles: ['HrOffice'] },
      { label: 'Configuration', path: '/admin/config', permission: 'admin.fees.manage', roles: ['Admin'] },
      { label: 'Encounters', path: '/doctor/encounters', permission: 'encounters.read', roles: ['Doctor'] },
      { label: 'Triage', path: '/nurse/triage', permission: 'vitals.write', roles: ['Nurse'] },
    ];
    return candidates.filter((x) => this.auth.hasAnyRole(x.roles) && this.auth.hasPermission(x.permission));
  });
}

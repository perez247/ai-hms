import { Component, computed, inject, signal, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { BrnDialogImports, BrnDialogRef, BrnDialogService } from '@spartan-ng/brain/dialog';
import { AuthService } from '../core/auth.service';
import { StaffRole } from '../core/models';

interface NavItem {
  label: string;
  path: string;
  permission?: string;
  roles: StaffRole[];
  strictRoles?: StaffRole[];
  icon: string;
  section: string;
}

// Navigation is generated from role + permission metadata; Admin passes every role check.
const NAV: NavItem[] = [
  { label: 'Patients', path: '/reception/patients', permission: 'patients.read', roles: ['Receptionist'], icon: '👥', section: 'Front desk' },
  { label: 'Patient profile', path: '/patients/lookup', permission: 'patients.profile.read', roles: ['Receptionist', 'Doctor', 'Pharmacy', 'Finance', 'HmoOffice', 'HrOffice'], icon: '🪪', section: 'Patient' },
  { label: 'Appointments', path: '/reception/appointments', permission: 'appointments.read', roles: ['Receptionist'], icon: '📅', section: 'Front desk' },
  { label: 'Triage', path: '/nurse/triage', permission: 'vitals.write', roles: ['Nurse'], icon: '🩺', section: 'Clinical' },
  { label: 'Encounters', path: '/doctor/encounters', permission: 'encounters.read', roles: ['Doctor'], icon: '📋', section: 'Clinical' },
  { label: 'Prescriptions', path: '/pharmacy/prescriptions', permission: 'pharmacy.read', roles: ['Pharmacy'], icon: '💊', section: 'Pharmacy' },
  { label: 'Bills', path: '/finance/bills', permission: 'billing.read', roles: ['Finance'], icon: '🧾', section: 'Finance' },
  { label: 'Account book', path: '/finance/account-book', permission: 'accountbook.read', roles: ['Finance'], icon: '📒', section: 'Finance' },
  { label: 'HMO desk', path: '/hmo/authorizations', permission: 'hmo.read', roles: ['HmoOffice'], icon: '🛡️', section: 'HMO' },
  { label: 'HMO partners', path: '/hmos', permission: 'fitness.read', roles: ['Finance', 'HmoOffice'], icon: '🏢', section: 'HMO' },
  { label: 'Fitness', path: '/fitness', permission: 'fitness.read', roles: ['Receptionist', 'Nurse', 'Doctor', 'Finance', 'HmoOffice'], icon: '🏃', section: 'Services' },
  { label: 'Handover', path: '/handover', roles: ['Nurse'], strictRoles: ['Nurse'], icon: '📝', section: 'Services' },
  { label: 'Users', path: '/hr/users', permission: 'hr.users.read', roles: ['HrOffice'], icon: '🧑‍💼', section: 'HR' },
  { label: 'Configuration', path: '/admin/config', permission: 'admin.fees.manage', roles: ['Admin'], icon: '⚙️', section: 'Admin' },
];

/** Sidebar/topbar shell shared by every authenticated role group. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, BrnDialogImports],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(BrnDialogService);
  private readonly viewContainer = inject(ViewContainerRef);
  private logoutRef?: BrnDialogRef<unknown>;

  @ViewChild('logoutDialog') private logoutDialog?: TemplateRef<unknown>;
  readonly mobileOpen = signal(false);
  readonly menuOpen = signal(false);
  readonly confirmLogout = signal(false);
  readonly nav = computed(() => NAV.filter((item) => {
    const strictOk = !item.strictRoles?.length || item.strictRoles.some((role) => this.auth.hasExactRole(role));
    const permissionOk = !item.permission || this.auth.hasPermission(item.permission);
    return strictOk && this.auth.hasAnyRole(item.roles) && permissionOk;
  }));
  readonly initials = computed(() => {
    const parts = (this.auth.user()?.fullName ?? 'User').trim().split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] ?? 'U') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
  });

  readonly sections = computed(() => {
    const grouped = new Map<string, NavItem[]>();
    for (const item of this.nav()) {
      const list = grouped.get(item.section) ?? [];
      list.push(item);
      grouped.set(item.section, list);
    }
    return Array.from(grouped.entries()).map(([name, items]) => ({ name, items }));
  });

  toggleMenu(): void { this.menuOpen.update((open) => !open); }

  openProfile(): void {
    this.menuOpen.set(false);
    void this.router.navigateByUrl('/staff/me');
  }

  requestLogout(): void {
    this.menuOpen.set(false);
    this.confirmLogout.set(true);
    if (!this.logoutDialog) return;
    this.logoutRef?.close();
    this.logoutRef = this.dialogs.open(this.logoutDialog, this.viewContainer, undefined,
      { ariaLabel: 'Sign out confirmation' });
  }

  cancelLogout(): void {
    this.logoutRef?.close();
    this.logoutRef = undefined;
    this.confirmLogout.set(false);
  }

  performLogout(): void {
    this.logoutRef?.close();
    this.logoutRef = undefined;
    this.confirmLogout.set(false);
    this.auth.logout();
  }
}

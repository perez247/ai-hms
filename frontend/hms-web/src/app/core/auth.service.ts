import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { API_BASE } from './api.config';
import { AuthResponse, MeResponse, StaffRole } from './models';

const TOKEN_KEY = 'hms.accessToken';
const REFRESH_KEY = 'hms.refreshToken';
const USER_KEY = 'hms.user';

/**
 * Owns the persisted session, permission set and role-aware landing route.
 * Components use signals instead of subscribing to a separate store.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly user = signal<MeResponse | null>(this.readUser());
  readonly isAuthenticated = computed(() => Boolean(this.token()));
  readonly permissions = computed(() => new Set(this.user()?.permissions ?? []));

  /** Authenticates and persists the access/refresh pair plus the permission snapshot. */
  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${API_BASE}/auth/login`, { email, password }).pipe(
      tap((response) => this.setSession(response)),
    );
  }

  /** App initializer: validates a stored token against /auth/me before rendering. */
  restoreSession(): Promise<void> {
    const token = this.token();
    if (!token) return Promise.resolve();
    // Always revalidate against the API; a cached role/permission snapshot may be stale
    // after role or permission changes in the backend.
    return new Promise((resolve) => {
      this.http.get<MeResponse>(`${API_BASE}/auth/me`).subscribe({
        next: (me) => {
          this.user.set(me);
          localStorage.setItem(USER_KEY, JSON.stringify(me));
          resolve();
        },
        error: () => {
          this.clearSession();
          resolve();
        },
      });
    });
  }

  setSession(response: AuthResponse): void {
    this.token.set(response.accessToken);
    this.user.set(response.user);
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
  }

  /** Clears local credentials; the interceptor can call this without forcing navigation. */
  logout(navigate = true): void {
    this.clearSession();
    if (navigate) void this.router.navigateByUrl('/login');
  }

  hasPermission(permission: string): boolean {
    return this.permissions().has(permission);
  }

  hasAnyPermission(permissions: string[]): boolean {
    return permissions.length === 0 || permissions.some((permission) => this.hasPermission(permission));
  }

  hasRole(role: StaffRole): boolean {
    const user = this.user();
    return Boolean(user && (user.role === 'Admin' || user.role === role));
  }

  hasAnyRole(roles: StaffRole[]): boolean {
    return roles.length === 0 || roles.some((role) => this.hasRole(role));
  }

  /** True only for the exact role; Admin is not treated as a member of other roles. */
  hasExactRole(role: StaffRole): boolean {
    return this.user()?.role === role;
  }

  homeForRole(role: StaffRole | undefined): string {
    switch (role) {
      case 'Receptionist': return '/reception/patients';
      case 'Nurse': return '/nurse/triage';
      case 'Doctor': return '/doctor/encounters';
      case 'Pharmacy': return '/pharmacy/prescriptions';
      case 'Finance': return '/finance/bills';
      case 'HmoOffice': return '/hmo/authorizations';
      case 'HrOffice': return '/hr/users';
      case 'Admin': return '/admin/config';
      default: return '/home';
    }
  }

  private clearSession(): void {
    this.token.set(null);
    this.user.set(null);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
  }

  private readUser(): MeResponse | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as MeResponse) : null;
    } catch {
      return null;
    }
  }
}

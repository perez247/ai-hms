import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { MeResponse } from '../core/models';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent sign-out confirmation', () => {
  const user = signal<MeResponse>({
    id: 'staff-1', fullName: 'Nurse Demo', role: 'Nurse', roleName: 'Nurse',
    permissions: ['vitals.read', 'vitals.write'],
  });
  const logout = vi.fn();
  const auth = {
    user,
    hasAnyRole: () => true,
    hasPermission: () => true,
    logout,
  };

  beforeEach(async () => {
    logout.mockClear();
    await TestBed.configureTestingModule({
      imports: [AppShellComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();
  });

  it('does not sign out until the confirmation is accepted', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    const component = fixture.componentInstance;

    component.requestLogout();
    expect(component.confirmLogout()).toBe(true);
    expect(logout).not.toHaveBeenCalled();

    component.cancelLogout();
    expect(component.confirmLogout()).toBe(false);
    expect(logout).not.toHaveBeenCalled();
  });

  it('clears the session only after explicit confirmation', () => {
    const fixture = TestBed.createComponent(AppShellComponent);
    const component = fixture.componentInstance;

    component.requestLogout();
    component.performLogout();

    expect(component.confirmLogout()).toBe(false);
    expect(logout).toHaveBeenCalledTimes(1);
  });
});

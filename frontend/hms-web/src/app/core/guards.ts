import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { StaffRole } from './models';

// Route-level gates: authentication first, then role + permission data from app.routes.ts.
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const accessGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);

  const roles = (route.data['roles'] as StaffRole[] | undefined) ?? [];
  const permissions = (route.data['permissions'] as string[] | undefined) ?? [];
  if (auth.hasAnyRole(roles) && auth.hasAnyPermission(permissions)) return true;
  return router.createUrlTree(['/forbidden']);
};

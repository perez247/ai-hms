import { Routes } from '@angular/router';

export const HR_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'users' },
  { path: 'users', loadComponent: () => import('./hr-users.component').then((m) => m.HrUsersComponent) },
];

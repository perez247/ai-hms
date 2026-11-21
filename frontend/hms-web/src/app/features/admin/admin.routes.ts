import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'config' },
  { path: 'config', loadComponent: () => import('./admin.component').then((m) => m.AdminComponent) },
];

import { Routes } from '@angular/router';

export const HMO_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'authorizations' },
  { path: 'authorizations', loadComponent: () => import('./hmo.component').then((m) => m.HmoComponent) },
];

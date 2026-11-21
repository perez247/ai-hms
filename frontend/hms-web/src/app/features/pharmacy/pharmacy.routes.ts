import { Routes } from '@angular/router';

export const PHARMACY_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'prescriptions' },
  { path: 'prescriptions', loadComponent: () => import('./pharmacy.component').then((m) => m.PharmacyComponent) },
];

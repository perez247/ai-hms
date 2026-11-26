import { Routes } from '@angular/router';

export const DOCTOR_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'encounters' },
  { path: 'encounters', loadComponent: () => import('./doctor.component').then((m) => m.DoctorComponent) },
  { path: 'encounters/:id', loadComponent: () => import('./encounter-detail.component').then((m) => m.EncounterDetailComponent) },
];

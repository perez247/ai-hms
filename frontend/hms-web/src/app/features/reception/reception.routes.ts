import { Routes } from '@angular/router';

export const RECEPTION_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'patients' },
  { path: 'patients', loadComponent: () => import('./patients.component').then((m) => m.PatientsComponent) },
  { path: 'appointments', loadComponent: () => import('./appointments.component').then((m) => m.AppointmentsComponent) },
];

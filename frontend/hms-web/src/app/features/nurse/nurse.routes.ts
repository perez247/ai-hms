import { Routes } from '@angular/router';

export const NURSE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'triage' },
  { path: 'triage', loadComponent: () => import('./nurse.component').then((m) => m.NurseComponent) },
];

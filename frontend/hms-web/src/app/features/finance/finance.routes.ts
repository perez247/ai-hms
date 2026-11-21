import { Routes } from '@angular/router';

export const FINANCE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'bills' },
  { path: 'bills', loadComponent: () => import('./bills.component').then((m) => m.BillsComponent) },
  { path: 'account-book', loadComponent: () => import('./account-book.component').then((m) => m.AccountBookComponent) },
];

import { Routes } from '@angular/router';
import { accessGuard } from '../../core/guards';

// Bills is shared by Finance and Pharmacy (approve, negotiate, settle); the account book remains Finance-only.
export const FINANCE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'bills' },
  {
    path: 'bills',
    canActivate: [accessGuard],
    data: { roles: ['Finance', 'Pharmacy'], permissions: ['billing.read'] },
    loadComponent: () => import('./bills.component').then((m) => m.BillsComponent),
  },
  {
    path: 'account-book',
    canActivate: [accessGuard],
    data: { roles: ['Finance'], permissions: ['accountbook.read'] },
    loadComponent: () => import('./account-book.component').then((m) => m.AccountBookComponent),
  },
];

import { Routes } from '@angular/router';

export const STOCKS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./stocks.component').then((m) => m.StocksComponent),
  },
];

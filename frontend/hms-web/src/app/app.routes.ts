import { Routes } from '@angular/router';
import { accessGuard, authGuard } from './core/guards';

// Public login plus one lazy route group per role. Access guards also check explicit permissions.
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      {
        path: 'home',
        loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'reception',
        canActivate: [accessGuard],
        data: { roles: ['Receptionist'], permissions: ['patients.read'] },
        loadChildren: () => import('./features/reception/reception.routes').then((m) => m.RECEPTION_ROUTES),
      },
      {
        path: 'nurse',
        canActivate: [accessGuard],
        data: { roles: ['Nurse'], permissions: ['vitals.read'] },
        loadChildren: () => import('./features/nurse/nurse.routes').then((m) => m.NURSE_ROUTES),
      },
      {
        path: 'doctor',
        canActivate: [accessGuard],
        data: { roles: ['Doctor'], permissions: ['encounters.read'] },
        loadChildren: () => import('./features/doctor/doctor.routes').then((m) => m.DOCTOR_ROUTES),
      },
      {
        path: 'pharmacy',
        canActivate: [accessGuard],
        data: { roles: ['Pharmacy'], permissions: ['pharmacy.read'] },
        loadChildren: () => import('./features/pharmacy/pharmacy.routes').then((m) => m.PHARMACY_ROUTES),
      },
      {
        path: 'finance',
        canActivate: [accessGuard],
        data: { roles: ['Finance', 'Pharmacy'], permissions: ['billing.read'] },
        loadChildren: () => import('./features/finance/finance.routes').then((m) => m.FINANCE_ROUTES),
      },
      {
        path: 'hmo',
        canActivate: [accessGuard],
        data: { roles: ['HmoOffice'], permissions: ['hmo.read'] },
        loadChildren: () => import('./features/hmo/hmo.routes').then((m) => m.HMO_ROUTES),
      },
      {
        path: 'lab',
        canActivate: [accessGuard],
        data: { roles: ['Lab', 'Pharmacy', 'Doctor', 'Admin'], permissions: ['lab.requests.read'] },
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'tests' },
          {
            path: 'tests',
            loadComponent: () => import('./features/results/result-queue.component').then((m) => m.ResultQueueComponent),
            data: { roles: ['Lab', 'Pharmacy', 'Doctor', 'Admin'], permissions: ['lab.requests.read'], kind: 'Test' },
          },
        ],
      },
      {
        path: 'procedures',
        canActivate: [accessGuard],
        data: { roles: ['Doctor', 'Pharmacy', 'Admin'], permissions: ['procedures.requests.read'], kind: 'Procedure' },
        loadComponent: () => import('./features/results/result-queue.component').then((m) => m.ResultQueueComponent),
      },
      {
        path: 'stocks',
        canActivate: [accessGuard],
        data: { roles: ['Pharmacy', 'Admin'], permissions: ['stock.read'] },
        loadChildren: () => import('./features/stocks/stocks.routes').then((m) => m.STOCKS_ROUTES),
      },
      {
        path: 'hr',
        canActivate: [accessGuard],
        data: { roles: ['HrOffice'], permissions: ['hr.users.read'] },
        loadChildren: () => import('./features/hr/hr.routes').then((m) => m.HR_ROUTES),
      },
      {
        path: 'admin',
        canActivate: [accessGuard],
        data: { roles: ['Admin'], permissions: ['admin.fees.manage'] },
        loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
      },
      {
        path: 'patients/lookup',
        canActivate: [accessGuard],
        data: { roles: ['Receptionist', 'Doctor', 'Pharmacy', 'Finance', 'HmoOffice', 'HrOffice'], permissions: ['patients.profile.read'] },
        loadComponent: () => import('./features/patients/patient-lookup.component').then((m) => m.PatientLookupComponent),
      },
      {
        path: 'patients/:patientNumber',
        canActivate: [accessGuard],
        data: { roles: ['Receptionist', 'Doctor', 'Pharmacy', 'Finance', 'HmoOffice', 'HrOffice'], permissions: ['patients.profile.read'] },
        loadComponent: () => import('./features/patients/patient-profile.component').then((m) => m.PatientProfileComponent),
      },
      {
        path: 'hmos',
        canActivate: [accessGuard],
        data: { roles: ['Finance', 'HmoOffice'], permissions: ['fitness.read'] },
        loadComponent: () => import('./features/hmo-directory/hmo-directory.component').then((m) => m.HmoDirectoryComponent),
      },
      {
        path: 'hmos/:id',
        canActivate: [accessGuard],
        data: { roles: ['Finance', 'HmoOffice'], permissions: ['fitness.read'] },
        loadComponent: () => import('./features/hmo-directory/hmo-profile.component').then((m) => m.HmoProfileComponent),
      },
      {
        path: 'handover',
        canActivate: [accessGuard],
        data: { roles: ['Nurse'], strictRoles: ['Nurse'] },
        loadComponent: () => import('./features/nurse/handover.component').then((m) => m.HandoverComponent),
      },
      {
        path: 'staff/me',
        loadComponent: () => import('./features/staff/staff-profile.component').then((m) => m.StaffProfileComponent),
      },
      {
        path: 'staff/:id',
        canActivate: [accessGuard],
        data: { permissions: ['hr.users.read'] },
        loadComponent: () => import('./features/staff/staff-profile.component').then((m) => m.StaffProfileComponent),
      },
      {
        path: 'fitness',
        canActivate: [accessGuard],
        data: { permissions: ['fitness.read'] },
        loadComponent: () => import('./features/fitness/fitness.component').then((m) => m.FitnessComponent),
      },
    ],
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/shared/forbidden.component').then((m) => m.ForbiddenComponent),
  },
  {
    path: '**',
    loadComponent: () => import('./features/shared/not-found.component').then((m) => m.NotFoundComponent),
  },
];

import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Rendered when a route guard rejects the current role/permission set. */
@Component({
  selector: 'app-forbidden',
  imports: [RouterLink],
  template: `
    <main class="grid min-h-screen place-items-center bg-slate-50 p-6">
      <div class="card max-w-md p-8 text-center">
        <p class="text-xs font-bold uppercase tracking-[0.2em] text-rose-600">Access denied</p>
        <h1 class="mt-3 text-3xl font-black text-slate-950">This workspace is not assigned to your role.</h1>
        <p class="mt-3 text-sm leading-6 text-slate-500">If you believe this is wrong, ask HR or Admin to review your role.</p>
        <a routerLink="/home" class="btn-primary mt-6">Return to your home</a>
      </div>
    </main>
  `,
})
export class ForbiddenComponent {}

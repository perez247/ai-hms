import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Fallback route for unknown URLs. */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  template: `
    <main class="grid min-h-screen place-items-center bg-slate-50 p-6">
      <div class="card max-w-md p-8 text-center">
        <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">404</p>
        <h1 class="mt-3 text-3xl font-black text-slate-950">Page not found.</h1>
        <a routerLink="/home" class="btn-primary mt-6">Go home</a>
      </div>
    </main>
  `,
})
export class NotFoundComponent {}

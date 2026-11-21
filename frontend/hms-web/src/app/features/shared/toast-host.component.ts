import { Component, inject } from '@angular/core';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-toast-host',
  template: `
    <div class="pointer-events-none fixed right-4 top-4 z-[100] flex w-full max-w-sm flex-col gap-2">
      @for (item of toast.messages(); track item.id) {
        <div class="pointer-events-auto flex items-start gap-3 rounded-xl border p-4 shadow-lg"
             [class]="item.kind === 'error' ? 'border-rose-200 bg-rose-50 text-rose-800' : item.kind === 'success' ? 'border-emerald-200 bg-emerald-50 text-emerald-800' : 'border-sky-200 bg-sky-50 text-sky-800'">
          <p class="flex-1 text-sm font-medium leading-5">{{ item.text }}</p>
          <button type="button" class="rounded p-0.5 text-xs font-bold opacity-60 hover:opacity-100" (click)="toast.dismiss(item.id)" aria-label="Dismiss">✕</button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  readonly toast = inject(ToastService);
}

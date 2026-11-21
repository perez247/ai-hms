import { Injectable, signal } from '@angular/core';

export type ToastKind = 'error' | 'success' | 'info';

export interface ToastMessage {
  id: number;
  kind: ToastKind;
  text: string;
}

/**
 * Minimal in-app toast queue. No external notification library is used; the host component
 * renders the stack and auto-dismisses transient messages.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly messages = signal<ToastMessage[]>([]);
  private nextId = 1;

  error(text: string): void { this.show(text, 'error', 7000); }
  success(text: string): void { this.show(text, 'success', 4500); }
  info(text: string): void { this.show(text, 'info', 4500); }

  dismiss(id: number): void {
    this.messages.update((items) => items.filter((item) => item.id !== id));
  }

  private show(text: string, kind: ToastKind, durationMs: number): void {
    const id = this.nextId++;
    this.messages.update((items) => [...items, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }
}

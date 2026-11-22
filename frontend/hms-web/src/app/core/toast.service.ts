import { Injectable } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

/**
 * Thin application facade over Spartan Brain Sonner. Components keep the existing
 * toast.error/success/info call convention while the presentation is handled by
 * BrnSonnerToaster at the bottom-right of the app shell.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  error(text: string): void { toast.error(text, { position: 'bottom-right' }); }
  success(text: string): void { toast.success(text, { position: 'bottom-right' }); }
  info(text: string): void { toast.info(text, { position: 'bottom-right' }); }
}

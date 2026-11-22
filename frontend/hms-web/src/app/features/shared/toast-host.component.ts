import { Component } from '@angular/core';
import { BrnSonnerToaster } from '@spartan-ng/brain/sonner';

/** Spartan Sonner host. Position is fixed at the bottom-right by design. */
@Component({
  selector: 'app-toast-host',
  imports: [BrnSonnerToaster],
  template: `<brn-sonner-toaster position="bottom-right" [richColors]="true" [closeButton]="true" [visibleToasts]="4" />`,
})
export class ToastHostComponent {}

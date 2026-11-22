import { Injectable, inject } from '@angular/core';
import { BrnDialogService } from '@spartan-ng/brain/dialog';
import { take } from 'rxjs';
import { PromptDialogComponent, PromptDialogData } from '../features/shared/prompt-dialog.component';

/** Opens a Spartan Dialog for a short structured input and resolves with the entered values. */
@Injectable({ providedIn: 'root' })
export class PromptService {
  private readonly dialogs = inject(BrnDialogService);

  open(data: PromptDialogData): Promise<Record<string, string> | null> {
    return new Promise((resolve) => {
      const ref = this.dialogs.open<PromptDialogData, Record<string, string>>(
        PromptDialogComponent, undefined, data, { ariaLabel: data.title });
      ref.closed$.pipe(take(1)).subscribe((result) => resolve(result ?? null));
    });
  }
}

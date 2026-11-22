import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrnDialogImports, BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';

export interface PromptField {
  key: string;
  label: string;
  type?: 'text' | 'number' | 'password';
  value?: string | number;
  placeholder?: string;
}

export interface PromptDialogData {
  title: string;
  description?: string;
  fields: PromptField[];
  confirmLabel?: string;
}

/** Generic Spartan dialog used for short structured inputs that previously used browser prompts. */
@Component({
  selector: 'app-prompt-dialog',
  imports: [FormsModule, BrnDialogImports],
  template: `
    <div class="w-[min(460px,92vw)] rounded-2xl bg-white p-6 shadow-2xl" >
      <h2 class="text-xl font-black text-slate-950" brnDialogTitle>{{ data.title }}</h2>
      @if (data.description) { <p class="mt-2 text-sm leading-6 text-slate-500" brnDialogDescription>{{ data.description }}</p> }
      <div class="mt-5 space-y-3">
        @for (field of data.fields; track field.key) {
          <div>
            <label class="label">{{ field.label }}</label>
            <input class="input" [type]="field.type || 'text'" [(ngModel)]="values[field.key]"
              [placeholder]="field.placeholder || ''" />
          </div>
        }
      </div>
      <div class="mt-6 flex justify-end gap-3">
        <button type="button" class="btn-secondary" (click)="dismiss()">Cancel</button>
        <button type="button" class="btn-primary" (click)="submit()">{{ data.confirmLabel || 'Confirm' }}</button>
      </div>
    </div>
  `,
})
export class PromptDialogComponent {
  readonly data = injectBrnDialogContext<PromptDialogData>();
  readonly values: Record<string, string> = {};
  private readonly ref = inject(BrnDialogRef<Record<string, string>>);

  constructor() {
    for (const field of this.data.fields) {
      this.values[field.key] = field.value === undefined || field.value === null ? '' : String(field.value);
    }
  }

  submit(): void { this.ref.close(this.values); }
  dismiss(): void { this.ref.close(undefined); }
}

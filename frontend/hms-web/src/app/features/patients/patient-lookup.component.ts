import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastService } from '../../core/toast.service';

/** Universal patient-profile entry point; all roles with patients.profile.read can use it. */
@Component({
  selector: 'app-patient-lookup',
  imports: [FormsModule],
  template: `
    <div class="mx-auto max-w-2xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Patient profile</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Open a patient record</h1>
      <p class="mt-1 text-sm text-slate-500">Enter a patient number. Clinical and financial sections are filtered by your role.</p>
      <div class="card mt-6 p-6">
        <label class="label">Patient number</label>
        <input class="input" [(ngModel)]="patientNumber" name="patientNumber" placeholder="e.g. PAT-2026-001234" />
        <button type="button" class="btn-primary mt-4 w-full" (click)="open()">Open profile</button>
      </div>
    </div>
  `,
})
export class PatientLookupComponent {
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  patientNumber = '';

  open(): void {
    const number = this.patientNumber.trim();
    if (!number) { this.toast.error('Enter a patient number.'); return; }
    void this.router.navigate(['/patients', number]);
  }
}

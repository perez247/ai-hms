import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { Encounter } from '../../core/models';

/** Triage vitals and inpatient medication administration evidence. */
@Component({
  selector: 'app-nurse',
  imports: [FormsModule, DatePipe],
  templateUrl: './nurse.component.html',
})
export class NurseComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly encounter = signal<Encounter | null>(null);
  readonly notice = signal<string | null>(null);
  encounterId = '';
  temperatureC: number | null = null;
  systolic: number | null = null;
  diastolic: number | null = null;
  pulse: number | null = null;
  respiratoryRate: number | null = null;
  spo2: number | null = null;
  weightKg: number | null = null;
  heightCm: number | null = null;
  painScore: number | null = null;
  vitalsNotes = '';
  dispenseItemId = '';
  doseGiven: number | null = null;
  route = 'Oral';
  batchNumber = '';
  administrationNotes = '';

  load(): void {
    if (!this.encounterId.trim()) return;
    this.api.clinical.encounter(this.encounterId.trim()).subscribe({
      next: (encounter) => this.encounter.set(encounter),
      error: (err) => { this.encounter.set(null); this.toast.error(err?.error?.detail ?? 'Encounter not found.'); },
    });
  }

  saveVitals(): void {
    const encounter = this.encounter();
    if (!encounter) return;
    this.api.clinical.vitals({
      encounterId: encounter.id, temperatureC: this.temperatureC, systolic: this.systolic, diastolic: this.diastolic,
      pulse: this.pulse, respiratoryRate: this.respiratoryRate, spo2: this.spo2, weightKg: this.weightKg,
      heightCm: this.heightCm, painScore: this.painScore, notes: this.vitalsNotes || null,
    }).subscribe({
      next: () => { this.notice.set('Vitals recorded.'); this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not record vitals.'),
    });
  }

  administer(): void {
    if (!this.dispenseItemId.trim() || !this.doseGiven || this.doseGiven <= 0) return;
    this.api.clinical.administer({
      dispenseItemId: this.dispenseItemId.trim(), doseGiven: Number(this.doseGiven),
      route: this.route, batchNumber: this.batchNumber || null, notes: this.administrationNotes || null,
    }).subscribe({
      next: () => { this.notice.set('Administration recorded. Inpatient medication charge accrued to the admission.'); this.dispenseItemId = ''; this.doseGiven = null; },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not record administration.'),
    });
  }
}

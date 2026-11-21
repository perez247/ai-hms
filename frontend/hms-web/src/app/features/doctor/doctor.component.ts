import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AuthService } from '../../core/auth.service';
import { Encounter } from '../../core/models';

/** Doctor encounter/admission lookup and creation; deep-links the current prescription to Pharmacy. */
@Component({
  selector: 'app-doctor',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './doctor.component.html',
})
export class DoctorComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly encounter = signal<Encounter | null>(null);
  readonly notice = signal<string | null>(null);
  encounterId = '';
  patientNumber = '';
  appointmentId = '';
  hmoEnrollmentId = '';
  chiefComplaint = '';
  isEmergency = false;
  wardType = 'General';
  bedNumber = '';
  admissionPatientNumber = '';

  load(): void {
    if (!this.encounterId.trim()) return;
    this.api.clinical.encounter(this.encounterId.trim()).subscribe({
      next: (encounter) => this.encounter.set(encounter),
      error: (err) => { this.encounter.set(null); this.toast.error(err?.error?.detail ?? 'Encounter not found.'); },
    });
  }

  createOutpatient(): void {
    if (!this.patientNumber.trim()) return;
    const doctorId = this.auth.user()?.id;
    if (!doctorId) return;
    this.api.patients.getByNumber(this.patientNumber.trim()).subscribe({
      next: (patient) => this.api.clinical.createOutpatient({
        patientId: patient.id, doctorId, appointmentId: this.appointmentId || null,
        isEmergency: this.isEmergency, hmoEnrollmentId: this.hmoEnrollmentId || null,
        chiefComplaint: this.chiefComplaint || null,
      }).subscribe({
        next: (encounter) => { this.encounter.set(encounter); this.encounterId = encounter.id; this.notice.set('Outpatient encounter created. Bill is in Draft until Pharmacy commits.'); },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not create encounter.'),
      }),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Patient number not found.'),
    });
  }

  createAdmission(): void {
    if (!this.admissionPatientNumber.trim()) return;
    const doctorId = this.auth.user()?.id;
    if (!doctorId) return;
    this.api.patients.getByNumber(this.admissionPatientNumber.trim()).subscribe({
      next: (patient) => this.api.clinical.createAdmission({
        patientId: patient.id, doctorId, wardType: this.wardType,
        bedNumber: this.bedNumber || null, hmoEnrollmentId: this.hmoEnrollmentId || null,
        chiefComplaint: this.chiefComplaint || null, admittedAt: new Date().toISOString(),
      }).subscribe({
        next: () => { this.notice.set('Admission created and consultation charge added.'); },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not create admission.'),
      }),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Patient number not found.'),
    });
  }
}

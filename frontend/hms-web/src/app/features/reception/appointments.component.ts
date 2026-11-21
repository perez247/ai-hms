import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { DoctorOption } from '../../core/models';

interface AppointmentRow {
  id: string; appointmentNumber: string; patientId: string; patientNumber: string; patientName: string; doctorName: string;
  scheduledAt: string; status: string; billId: string | null;
}

/** Reception booking and check-in; booking creates the appointment fee bill server-side. */
@Component({
  selector: 'app-appointments',
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <div class="mx-auto max-w-7xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Reception</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Appointments</h1>
      <p class="mt-1 text-sm text-slate-500">Book a doctor and automatically send the appointment fee to Finance.</p>
      @if (notice()) { <div class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{{ notice() }}</div> }

      <div class="mt-5 grid gap-5 lg:grid-cols-[380px_1fr]">
        <form class="card h-fit" [formGroup]="form" (ngSubmit)="create()">
          <div class="card-header"><h2 class="font-bold text-slate-900">Book appointment</h2></div>
          <div class="card-body space-y-4">
            <div><label class="label">Patient number</label><input class="input" formControlName="patientNumber" placeholder="e.g. PAT-2026-001234" /></div>
            <div><label class="label">Doctor</label><select class="input" formControlName="doctorId"><option value="">Select doctor</option>@for (doctor of doctors(); track doctor.id) {<option [value]="doctor.id">{{ doctor.fullName }}</option>}</select></div>
            <div><label class="label">Date and time</label><input class="input" type="datetime-local" formControlName="scheduledAt" /></div>
            <div><label class="label">Duration (minutes)</label><input class="input" type="number" formControlName="durationMinutes" /></div>
            <div><label class="label">Reason</label><input class="input" formControlName="reason" /></div>
            <button class="btn-primary w-full" type="submit" [disabled]="form.invalid">Book and charge</button>
          </div>
        </form>

        <div class="card overflow-hidden">
          <div class="flex items-center justify-between border-b border-slate-100 p-4"><h2 class="font-bold text-slate-900">Booked appointments</h2><button class="btn-secondary" type="button" (click)="load()">Refresh</button></div>
          <div class="overflow-x-auto">
            <table class="table">
              <thead><tr><th>Appointment</th><th>Patient</th><th>Doctor</th><th>Scheduled</th><th>Status</th><th></th></tr></thead>
              <tbody>
                @for (appointment of items(); track appointment.id) {
                  <tr>
                    <td class="font-mono text-xs">{{ appointment.appointmentNumber }}</td>
                    <td><p class="font-semibold">{{ appointment.patientName }}</p><p class="text-xs text-slate-500">{{ appointment.patientNumber }}</p></td><td>{{ appointment.doctorName }}</td>
                    <td>{{ appointment.scheduledAt | date:'medium' }}</td>
                    <td><span class="badge-blue">{{ appointment.status }}</span></td>
                    <td class="text-right">@if (appointment.status === 'Scheduled') {<button class="btn-secondary" type="button" (click)="checkIn(appointment.id)">Check in</button>}</td>
                  </tr>
                }
                @if (items().length === 0) { <tr><td colspan="6" class="py-10 text-center text-slate-500">No appointments booked.</td></tr> }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class AppointmentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly doctors = signal<DoctorOption[]>([]);
  readonly items = signal<AppointmentRow[]>([]);
  readonly notice = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    patientNumber: ['', Validators.required],
    doctorId: ['', Validators.required],
    scheduledAt: ['', Validators.required],
    durationMinutes: [30, Validators.required],
    reason: [''],
  });

  ngOnInit(): void {
    this.api.reference.doctors().subscribe({ next: (doctors) => this.doctors.set(doctors) });
    this.load();
  }

  load(): void {
    this.api.get<{ items: AppointmentRow[] }>('/appointments?pageSize=50').subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load appointments.'),
    });
  }

  create(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.api.patients.getByNumber(raw.patientNumber.trim()).subscribe({
      next: (patient) => this.api.post('/appointments', {
        patientId: patient.id, doctorId: raw.doctorId,
        scheduledAt: new Date(raw.scheduledAt).toISOString(),
        durationMinutes: Number(raw.durationMinutes), reason: raw.reason || null,
      }).subscribe({
        next: () => { this.notice.set('Appointment booked. Appointment fee is committed to Finance.'); this.load(); this.form.reset({ durationMinutes: 30 }); },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not book appointment.'),
      }),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Patient number not found.'),
    });
  }

  checkIn(id: string): void {
    this.api.post(`/appointments/${id}/check-in`, {}).subscribe({
      next: () => { this.notice.set('Patient checked in.'); this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Check-in failed.'),
    });
  }
}

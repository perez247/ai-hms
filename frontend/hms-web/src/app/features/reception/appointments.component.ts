import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AppointmentPriceQuote, DoctorOption, HmoEnrollmentOptionFull, OngoingAppointment } from '../../core/models';

interface AppointmentRow {
  id: string; appointmentNumber: string; patientId: string; patientNumber: string; patientName: string; doctorName: string;
  scheduledAt: string; status: string; billId: string | null;
}

/** Reception booking and check-in; booking creates the appointment fee bill server-side. */
@Component({
  selector: 'app-appointments',
  imports: [ReactiveFormsModule, DatePipe, CurrencyPipe],
  template: `
    <div class="mx-auto px-4">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Reception</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Appointments</h1>
      <p class="mt-1 text-sm text-slate-500">Book a doctor and automatically send the appointment fee to Finance.</p>
      @if (notice()) { <div class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">{{ notice() }}</div> }
      @if (blockedMessage()) { <div class="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700"><p class="font-bold">Booking blocked</p><p>{{ blockedMessage() }}</p></div> }

      <div class="mt-5 grid gap-5 lg:grid-cols-[380px_1fr]">
        <form class="card h-fit" [formGroup]="form" (ngSubmit)="create()">
          <div class="card-header"><h2 class="font-bold text-slate-900">Book appointment</h2></div>
          <div class="card-body space-y-4">
            <div><label class="label">Patient number</label><input class="input" formControlName="patientNumber" placeholder="e.g. PAT-2026-001234" (blur)="checkOngoing()" /></div>
            <div><label class="label">Doctor</label><select class="input" formControlName="doctorId"><option value="">Select doctor</option>@for (doctor of doctors(); track doctor.id) {<option [value]="doctor.id">{{ doctor.fullName }}</option>}</select></div>
            <div><label class="label">Date and time</label><input class="input" type="datetime-local" formControlName="scheduledAt" /></div>
            <div><label class="label">Duration (minutes)</label><input class="input" type="number" formControlName="durationMinutes" /></div>
            <div><label class="label">Reason</label>
              <select class="input" formControlName="reason">
                <option value="Consultation">Consultation</option>
                <option value="Emergency">Emergency</option>
              </select>
            </div>
            <div>
              <label class="label">Funding source</label>
              <select class="input" formControlName="fundingSource">
                <option value="Private">Private — patient pays</option>
                <option value="Hmo" [disabled]="enrollments().length === 0">HMO — enrolled payer</option>
              </select>
            </div>
            @if (form.controls.fundingSource.value === 'Hmo') {
              <div>
                <label class="label">Patient's HMO enrollment</label>
                <select class="input" formControlName="hmoEnrollmentId">
                  <option value="">Select enrollment</option>
                  @for (enrollment of enrollments(); track enrollment.id) {
                    <option [value]="enrollment.id">{{ enrollment.hmoName }} · {{ enrollment.hmoMemberId }}{{ enrollment.isPrimary ? ' (primary)' : '' }}</option>
                  }
                </select>
                @if (enrollments().length === 0) { <p class="mt-1 text-xs text-rose-600">No active HMO enrollments for this patient.</p> }
              </div>
            }
            @if (quote(); as price) {
              <div class="rounded-xl border border-teal-200 bg-teal-50 p-3 text-sm text-teal-900">
                <p class="font-bold">{{ price.reason }} fee · {{ price.payerName }}</p>
                <p class="mt-1 text-xs">Total {{ price.unitPrice | currency:'NGN':'symbol-narrow':'1.2-2' }}</p>
                <p class="text-xs">HMO covers {{ price.hmoAmount | currency:'NGN':'symbol-narrow':'1.2-2' }} · Patient pays {{ price.patientAmount | currency:'NGN':'symbol-narrow':'1.2-2' }}</p>
                @if (!price.isConfigured) { <p class="mt-1 text-xs text-amber-700">Using the legacy fee configuration until a catalogue price is set.</p> }
              </div>
            }
            <button class="btn-primary w-full" type="submit" [disabled]="form.invalid || !!blockedMessage()">Book and charge</button>
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
  readonly enrollments = signal<HmoEnrollmentOptionFull[]>([]);
  readonly blockedMessage = signal<string | null>(null);
  readonly quote = signal<AppointmentPriceQuote | null>(null);
  readonly form = this.fb.nonNullable.group({
    patientNumber: ['', Validators.required],
    doctorId: ['', Validators.required],
    scheduledAt: ['', Validators.required],
    durationMinutes: [30, Validators.required],
    reason: ['Consultation', Validators.required],
    fundingSource: ['Private'],
    hmoEnrollmentId: [''],
  });

  ngOnInit(): void {
    this.api.reference.doctors().subscribe({ next: (doctors) => this.doctors.set(doctors) });
    this.form.controls.patientNumber.valueChanges.subscribe((number) => {
      this.blockedMessage.set(null);
      this.loadEnrollments(number);
    });
    this.form.valueChanges.subscribe(() => this.loadQuote());
    this.loadQuote();
    this.load();
  }

  private loadEnrollments(patientNumber: string): void {
    if (!patientNumber.trim()) { this.enrollments.set([]); this.loadQuote(); return; }
    this.api.patients.getByNumber(patientNumber.trim()).subscribe({
      next: (details) => this.api.reference.enrollments(details.patient.id).subscribe({
        next: (rows) => {
          const enrollments = rows as HmoEnrollmentOptionFull[];
          this.enrollments.set(enrollments);
          if (enrollments.length === 1) this.form.controls.hmoEnrollmentId.setValue(enrollments[0].id);
          this.loadQuote();
        },
        error: () => { this.enrollments.set([]); this.loadQuote(); },
      }),
      error: () => { this.enrollments.set([]); this.loadQuote(); },
    });
  }

  /** Reason and payer changes re-price the appointment immediately. */
  private loadQuote(): void {
    const raw = this.form.getRawValue();
    if (!raw.reason) { this.quote.set(null); return; }
    const hmoEnrollmentId = raw.fundingSource === 'Hmo' ? raw.hmoEnrollmentId || null : null;
    if (raw.fundingSource === 'Hmo' && !hmoEnrollmentId) { this.quote.set(null); return; }
    this.api.appointments.quote(raw.reason, raw.fundingSource, hmoEnrollmentId).subscribe({
      next: (quote) => this.quote.set(quote),
      error: () => this.quote.set(null),
    });
  }

  load(): void {
    this.api.get<{ items: AppointmentRow[] }>('/appointments?pageSize=50').subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => void err,
    });
  }

  /** Proactive UI check: the backend still enforces this on create. */
  checkOngoing(): void {
    const patientNumber = this.form.controls.patientNumber.value.trim();
    if (!patientNumber) { this.blockedMessage.set(null); return; }
    this.api.appointments.ongoing(patientNumber).subscribe({
      next: (ongoing: OngoingAppointment | null) => this.blockedMessage.set(ongoing ? this.ongoingMessage(ongoing) : null),
      error: () => this.blockedMessage.set(null),
    });
  }

  private ongoingMessage(ongoing: OngoingAppointment): string {
    return `This patient already has an ongoing appointment (${ongoing.appointmentNumber}, ${new Date(ongoing.scheduledAt).toLocaleString()}, ${ongoing.status}). ` +
      'The visit bill must be paid or invoiced to the HMO before a new appointment can be booked.';
  }

  create(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    if (raw.fundingSource === 'Hmo' && !raw.hmoEnrollmentId) {
      this.toast.error('Select the patient\'s HMO enrollment before booking.');
      return;
    }
    this.api.appointments.create({
        patientId: raw.patientNumber, doctorId: raw.doctorId,
        scheduledAt: new Date(raw.scheduledAt).toISOString(),
        durationMinutes: Number(raw.durationMinutes), reason: raw.reason,
        fundingSource: raw.fundingSource,
        hmoEnrollmentId: raw.fundingSource === 'Hmo' ? raw.hmoEnrollmentId || null : null,
      }).subscribe({
        next: () => {
          this.blockedMessage.set(null);
          this.notice.set('Appointment booked with verified funding. The visit bill is created at check-in.');
          this.load();
          this.form.reset({ durationMinutes: 30, reason: 'Consultation', fundingSource: 'Private', hmoEnrollmentId: '' });
          this.enrollments.set([]);
          this.loadQuote();
        },
        error: (err) => {
          const detail = (err as { error?: { detail?: string } })?.error?.detail;
          if (detail) this.blockedMessage.set(detail);
        },
      });
  }

  checkIn(id: string): void {
    this.api.appointments.checkIn(id).subscribe({
      next: () => { this.notice.set('Patient checked in.'); this.load(); },
      error: (err) => void err,
    });
  }
}

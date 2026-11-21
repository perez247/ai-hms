import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { HmoOption, Patient, PatientSearchItem } from '../../core/models';

/** Reception patient search/registration/renewal; registration triggers the fee bill server-side. */
@Component({
  selector: 'app-patients',
  imports: [ReactiveFormsModule, DatePipe, RouterLink],
  templateUrl: './patients.component.html',
})
export class PatientsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly items = signal<PatientSearchItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly notice = signal<string | null>(null);
  readonly showCreate = signal(false);
  readonly selected = signal<Patient | null>(null);
  readonly hmos = signal<HmoOption[]>([]);
  readonly search = signal('');

  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    middleName: [''],
    lastName: ['', Validators.required],
    sex: ['Female', Validators.required],
    dateOfBirth: ['', Validators.required],
    phone: [''],
    email: [''],
    address: [''],
    nextOfKinName: [''],
    nextOfKinPhone: [''],
    hmoCompanyId: [''],
    hmoMemberId: [''],
    planName: [''],
  });

  ngOnInit(): void {
    this.api.reference.hmos().subscribe({ next: (hmos) => this.hmos.set(hmos) });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.patients.search(this.search(), 1, 50)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (page) => { this.items.set(page.items); this.total.set(page.total); },
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load patients.'),
      });
  }

  searchChanged(value: string): void {
    this.search.set(value);
    this.load();
  }

  open(patient: PatientSearchItem): void {
    this.api.patients.get(patient.id).subscribe({
      next: (detail) => this.selected.set(detail),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load patient.'),
    });
  }

  renew(): void {
    const patient = this.selected();
    if (!patient) return;
    this.api.patients.renew(patient.id).subscribe({
      next: (result) => {
        this.notice.set(`Registration renewed to ${result.periodEnd}. Bill ${result.billId} sent to Finance.`);
        this.open({ id: patient.id } as PatientSearchItem);
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Renewal failed.'),
    });
  }

  create(): void {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    const today = new Date().toISOString().slice(0, 10);
    const oneYear = new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString().slice(0, 10);
    const body = {
      firstName: value.firstName, middleName: value.middleName || null, lastName: value.lastName,
      sex: value.sex, dateOfBirth: value.dateOfBirth,
      phone: value.phone || null, email: value.email || null, address: value.address || null,
      nextOfKinName: value.nextOfKinName || null, nextOfKinPhone: value.nextOfKinPhone || null,
      hmoEnrollments: value.hmoCompanyId && value.hmoMemberId ? [{
        hmoCompanyId: value.hmoCompanyId, hmoMemberId: value.hmoMemberId, planName: value.planName || null,
        validFrom: today, validTo: oneYear, isPrimary: true,
      }] : [],
    };
    this.api.patients.create(body).subscribe({
      next: (patient) => {
        this.notice.set(`Patient ${patient.patientNumber} created. Registration bill is ready for Finance.`);
        this.showCreate.set(false);
        this.form.reset({ sex: 'Female' });
        this.selected.set(patient);
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not create patient.'),
    });
  }
}

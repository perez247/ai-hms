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
  readonly editing = signal(false);
  readonly editingPatient = signal<Patient | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    middleName: [''],
    lastName: ['', Validators.required],
    sex: ['Female', Validators.required],
    dateOfBirth: ['', Validators.required],
    phone: [''],
    email: [''],
    address: [''],
    stateOfOrigin: [''],
    homeTown: [''],
    allergy: [''],
    occupation: [''],
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
        error: (err) => void err,
      });
  }

  searchChanged(value: string): void {
    this.search.set(value);
    this.load();
  }

  open(patient: PatientSearchItem): void {
    this.api.patients.get(patient.id).subscribe({
      next: (detail) => this.selected.set(detail),
      error: (err) => void err,
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
      error: (err) => void err,
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
      stateOfOrigin: value.stateOfOrigin || null, homeTown: value.homeTown || null,
      allergy: value.allergy || null, occupation: value.occupation || null,
      nextOfKinName: value.nextOfKinName || null, nextOfKinPhone: value.nextOfKinPhone || null,
      hmoEnrollments: value.hmoCompanyId && value.hmoMemberId ? [{
        hmoCompanyId: value.hmoCompanyId, hmoMemberId: value.hmoMemberId, planName: value.planName || null,
        validFrom: today, validTo: oneYear, isPrimary: true,
      }] : [],
    };
    const onDone = (patient: Patient) => {
      this.notice.set(this.editing()
        ? `Patient ${patient.patientNumber} updated.`
        : `Patient ${patient.patientNumber} created. Registration bill is ready for Finance.`);
      this.showCreate.set(false);
      this.editing.set(false);
      this.editingPatient.set(null);
      this.form.reset({ sex: 'Female' });
      this.selected.set(patient);
      this.load();
    };
    this.editing()
      ? this.api.patients.update(this.editingPatient()?.id ?? '', body).subscribe({
        next: (patient) => onDone(patient),
        error: (err) => void err,
      })
      : this.api.patients.create(body).subscribe({
        next: onDone,
        error: (err) => void err,
      });
  }

  startEdit(patient: Patient): void {
    this.editingPatient.set(patient);
    this.editing.set(true);
    this.showCreate.set(true);
    this.form.reset({
      firstName: patient.firstName, middleName: patient.middleName ?? '', lastName: patient.lastName,
      sex: patient.sex, dateOfBirth: patient.dateOfBirth, phone: patient.phone ?? '',
      email: patient.email ?? '', address: patient.address ?? '',
      stateOfOrigin: patient.stateOfOrigin ?? '', homeTown: patient.homeTown ?? '',
      allergy: patient.allergy ?? '', occupation: patient.occupation ?? '',
      nextOfKinName: patient.nextOfKinName ?? '', nextOfKinPhone: patient.nextOfKinPhone ?? '',
      hmoCompanyId: '', hmoMemberId: '', planName: '',
    });
  }
}

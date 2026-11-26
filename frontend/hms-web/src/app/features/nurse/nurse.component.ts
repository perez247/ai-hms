import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { PromptService } from '../../core/prompt.service';
import { ToastService } from '../../core/toast.service';
import { NurseAppointment, NurseEncounter, NurseInpatient, Patient, PatientSearchItem, PatientVitalsPage } from '../../core/models';
import { bpClass, isVitalAbnormal, vitalClass, VitalReading } from '../../core/vitals.config';

type NurseTab = 'walkin' | 'scheduled' | 'inpatients' | 'services';

/** Triage workspace: walk-in vitals, scheduled appointments and active inpatients. */
@Component({
  selector: 'app-nurse',
  imports: [FormsModule, DatePipe],
  templateUrl: './nurse.component.html',
})
export class NurseComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly prompts = inject(PromptService);

  readonly tab = signal<NurseTab>('walkin');
  readonly searchTerm = signal('');
  readonly results = signal<PatientSearchItem[]>([]);
  readonly patient = signal<Patient | null>(null);
  readonly appointments = signal<NurseAppointment[]>([]);
  readonly inpatients = signal<NurseInpatient[]>([]);
  readonly vitals = signal<PatientVitalsPage | null>(null);
  readonly encounters = signal<NurseEncounter[] | null>(null);
  readonly activeAppointment = signal<NurseAppointment | null>(null);
  readonly activeInpatient = signal<NurseInpatient | null>(null);

  readonly vitalClass = vitalClass;
  readonly bpClass = bpClass;
  readonly isVitalAbnormal = isVitalAbnormal;
  abnormalBadge(vital: VitalReading): string {
    return this.isVitalAbnormal(vital) ? 'Abnormal' : 'Normal';
  }

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

  ngOnInit(): void {
    this.loadAppointments();
    this.loadInpatients();
  }

  setTab(tab: NurseTab): void {
    this.tab.set(tab);
    if (tab !== 'services') {
      this.activeAppointment.set(null);
      this.activeInpatient.set(null);
    }
    if (tab === 'walkin' && this.patient()) this.loadVitals();
    if (tab === 'services' && this.patient()) this.loadEncounters();
  }

  search(): void {
    const term = this.searchTerm().trim();
    if (!term) { this.toast.error('Enter a patient number, name or phone.'); return; }
    this.api.patients.search(term, 1, 20).subscribe({
      next: (page) => this.results.set(page.items),
      error: (err) => void err,
    });
  }

  select(item: PatientSearchItem): void { this.selectPatient(item.patientNumber); }

  selectPatient(patientNumber: string, context?: { appointment?: NurseAppointment; inpatient?: NurseInpatient; services?: boolean }): void {
    this.api.patients.getByNumber(patientNumber).subscribe({
      next: (patient) => {
        this.patient.set(patient.patient);
        this.results.set([]);
        this.searchTerm.set('');
        this.activeAppointment.set(context?.appointment ?? null);
        this.activeInpatient.set(context?.inpatient ?? null);
        this.tab.set(context?.services ? 'services' : 'walkin');
        if (context?.services) this.loadEncounters(); else this.loadVitals();
      },
      error: (err) => void err,
    });
  }

  loadAppointments(): void {
    this.api.nurse.appointments().subscribe({
      next: (rows) => this.appointments.set(rows),
      error: (err) => void err,
    });
  }

  loadInpatients(): void {
    this.api.nurse.inpatients().subscribe({
      next: (rows) => this.inpatients.set(rows),
      error: (err) => void err,
    });
  }

  loadVitals(): void {
    const patient = this.patient();
    if (!patient) return;
    this.api.nurse.vitals(patient.patientNumber, 1, 5).subscribe({
      next: (page) => this.vitals.set(page),
      error: (err) => void err,
    });
  }

  saveVitals(): void {
    const patient = this.patient();
    if (!patient) return;
    this.api.nurse.recordVitals(patient.patientNumber, {
      patientNumber: patient.patientNumber, temperatureC: this.temperatureC, systolic: this.systolic,
      diastolic: this.diastolic, pulse: this.pulse, respiratoryRate: this.respiratoryRate,
      spo2: this.spo2, weightKg: this.weightKg, heightCm: this.heightCm, painScore: this.painScore,
      notes: this.vitalsNotes || null,
    }).subscribe({
      next: () => { this.toast.success('Vitals recorded.'); this.loadVitals(); },
      error: (err) => void err,
    });
  }

  loadEncounters(): void {
    const patient = this.patient();
    if (!patient) return;
    this.api.nurse.encounters(patient.patientNumber).subscribe({
      next: (rows) => this.encounters.set(rows),
      error: (err) => void err,
    });
  }

  openServices(row: NurseInpatient): void { this.selectPatient(row.patientNumber, { inpatient: row, services: true }); }

  async administer(item: NurseEncounter['prescriptions'][number]): Promise<void> {
    if (!item.dispenseItemId) { this.toast.error('Pharmacy must dispense this item first.'); return; }
    const values = await this.prompts.open({
      title: `Administer ${item.medicationName}`,
      description: `Prescribed ${item.quantityPrescribed} ${item.dose ?? ''} via ${item.route}.`,
      fields: [{ key: 'dose', label: 'Quantity administered', type: 'number', value: 1 }],
      confirmLabel: 'Log administration',
    });
    if (!values) return;
    const dose = Number(values['dose']);
    if (!dose || dose <= 0) { this.toast.error('Enter a valid administered quantity.'); return; }
    this.api.clinical.administer({
      dispenseItemId: item.dispenseItemId, doseGiven: dose, route: item.route,
      batchNumber: null, notes: this.activeInpatient()?.admissionNumber ?? 'Nurse triage',
    }).subscribe({
      next: () => { this.toast.success('Administration logged and billed.'); this.loadEncounters(); },
      error: (err) => void err,
    });
  }

  backToInpatients(): void {
    this.tab.set('inpatients');
    this.activeInpatient.set(null);
  }
}

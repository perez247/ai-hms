import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { Encounter, Patient, SaveEncounterRequest, ServiceRequestSummary } from '../../core/models';
import { ClerkingContext, ClerkingOrder, ClerkingPrescription, Disposition, DurationUnit, MedicationFrequency, MedicationRoute, OrderPriority } from './clerking.models';
import { ClerkingFormComponent } from './clerking-form.component';
import { bpClass, isVitalAbnormal, vitalClass } from '../../core/vitals.config';

/**
 * Full encounter page: SOAP summary, returned results, clerking form and a separate
 * accordion with the patient's earlier encounters (most recent first).
 */
@Component({
  selector: 'app-encounter-detail',
  imports: [DatePipe, RouterLink, ClerkingFormComponent],
  templateUrl: './encounter-detail.component.html',
})
export class EncounterDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  @ViewChild(ClerkingFormComponent) private formRef?: ClerkingFormComponent;

  private pendingAddNew = false;

  readonly encounter = signal<Encounter | null>(null);
  readonly patient = signal<Patient | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly notice = signal<string | null>(null);

  readonly vitalClass = vitalClass;
  readonly bpClass = bpClass;
  readonly isVitalAbnormal = isVitalAbnormal;
  readonly Math = Math;

  readonly returnedResults = computed(() =>
    (this.encounter()?.serviceRequests ?? []).filter((request) =>
      request.resultStatus !== null && request.resultStatus !== 'Pending'));

  readonly priorEncounters = computed(() => this.encounter()?.priorEncounters ?? []);

  /** Paginated returned results for the expanded prior encounter. */
  readonly priorResults = signal<ServiceRequestSummary[]>([]);
  readonly priorResultsPage = signal(1);
  readonly priorResultsTotal = signal(0);
  readonly priorResultsLoading = signal(false);
  readonly priorResultsId = signal<string | null>(null);

  loadPriorResults(id: string, page = 1): void {
    this.priorResultsId.set(id);
    this.priorResultsLoading.set(true);
    this.api.clinical.encounterResults(id, page).subscribe({
      next: (results) => {
        this.priorResults.set(results.items);
        this.priorResultsPage.set(results.page);
        this.priorResultsTotal.set(results.total);
        this.priorResultsLoading.set(false);
      },
      error: () => this.priorResultsLoading.set(false),
    });
  }

  onPriorResultsPage(page: number): void {
    const id = this.priorResultsId();
    if (id) this.loadPriorResults(id, page);
  }

  /** Objective section shows only the five most recent patient readings. */
  readonly latestVitals = computed(() => (this.encounter()?.vitals ?? []).slice(0, 5));

  /** Clerking is an input workflow: completed/cancelled encounters are read-only and hide the form. */
  readonly showClerking = computed(() => {
    const status = this.encounter()?.status;
    return !!status && status !== 'Completed' && status !== 'Cancelled';
  });

  readonly clerkingContext = computed<ClerkingContext | null>(() => {
    const encounter = this.encounter();
    const patient = this.patient();
    if (!encounter || !patient) return null;
    return {
      encounterId: encounter.id,
      encounterNumber: encounter.encounterNumber,
      encounterType: encounter.type,
      patient,
      presentingComplaint: encounter.presentingComplaint,
      historyOfPresentingComplaint: encounter.historyOfPresentingComplaint,
      pastMedicalSurgicalHistory: encounter.pastMedicalSurgicalHistory,
      familyHistory: encounter.familyHistory,
      generalObservation: encounter.clerking?.generalObservation ?? null,
      diagnosis: encounter.diagnosis,
      clinicalImpression: encounter.clerking?.clinicalImpression ?? null,
      disposition: (encounter.disposition ?? null) as Disposition | null,
      examination: encounter.examination ?? null,
      rosNotes: encounter.clerking?.rosNotes ?? null,
      allergies: encounter.clerking?.allergies ?? null,
      pointOfCare: encounter.clerking?.pointOfCare ?? null,
      planAdvice: encounter.clerking?.planAdvice ?? null,
      followUpDate: encounter.clerking?.followUpDate ?? null,
      referralDetails: encounter.clerking?.referralDetails ?? null,
      prescriptions: this.editableMedications(encounter),
      orders: this.planOrders(encounter, 'Test'),
      procedureRequests: this.planOrders(encounter, 'Procedure'),
      locked: encounter.status === 'Completed' || encounter.status === 'Cancelled',
    };
  });

  /** Mirrors the server rule: the newest un-dispensed prescription is the editable one. */
  private editableMedications(encounter: Encounter): ClerkingPrescription[] {
    const editable = [...(encounter.prescriptions ?? [])]
      .filter((prescription) => prescription.status !== 'Cancelled')
      .sort((a, b) => (b.createdAt ?? '').localeCompare(a.createdAt ?? ''))
      .find((prescription) => (prescription.items ?? []).every((item) => (item.quantityDispensed ?? 0) === 0));
    return (editable?.items ?? []).map((item) => ({
      id: item.id,
      medicationId: item.stockItemId,
      medicationName: item.medicationName,
      dosage: item.dose,
      route: item.route as MedicationRoute,
      frequency: item.frequency as MedicationFrequency,
      durationValue: item.durationDays,
      durationUnit: 'Days' as DurationUnit,
      quantity: item.quantityPrescribed,
      instructions: item.instructions ?? '',
    }));
  }

  private planOrders(encounter: Encounter, kind: 'Test' | 'Procedure'): ClerkingOrder[] {
    return (encounter.serviceRequests ?? [])
      .filter((request) => request.kind === kind && request.status !== 'Cancelled')
      .map((request) => ({
        id: request.id,
        testId: request.stockItemId,
        testName: request.itemName,
        priority: (request.priority as OrderPriority) || 'Routine',
        clinicalNote: request.clinicalNote ?? '',
      }));
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.load(id);
  }

  load(id: string): void {
    this.loading.set(true);
    this.api.clinical.encounter(id).subscribe({
      next: (encounter) => {
        this.encounter.set(encounter);
        this.loading.set(false);
        this.api.patients.getByNumber(encounter.patientNumber).subscribe({
          next: (details) => this.patient.set(details.patient),
          error: () => this.patient.set(null),
        });
      },
      error: () => { this.loading.set(false); this.encounter.set(null); },
    });
  }

  onDraftSaved(payload: SaveEncounterRequest): void {
    this.persist(payload, 'Clerking draft saved.');
  }

  onSigned(payload: SaveEncounterRequest): void {
    this.persist(payload, 'Encounter signed and locked.', () => {
      const encounter = this.encounter();
      const discharged = payload.disposition === 'Discharge' && encounter?.type === 'Inpatient';
      if (encounter && payload.disposition === 'Admit' && encounter.type === 'Outpatient') {
        this.notice.set('Encounter signed. Patient admitted — the new inpatient encounter is on the worklist.');
      } else if (discharged) {
        this.notice.set('Encounter signed. Treatment closed and billing started; Finance clearance completes the discharge.');
      }
      if (!this.pendingAddNew) return;
      this.pendingAddNew = false;
      // Discharge closes the admission, so no further encounter can be opened on it.
      if (discharged) return;
      if (encounter?.admissionId) this.createNextEncounter(encounter.admissionId, encounter.doctorId);
    });
  }

  onValidationFailed(): void {
    this.pendingAddNew = false;
    this.toast.error('Complete the highlighted fields before saving or signing the encounter.');
  }

  /** Inpatient only: sign the current encounter (if open), then open a fresh encounter on the same admission. */
  addNewEncounter(): void {
    const encounter = this.encounter();
    if (!encounter?.admissionId || this.busy()) return;
    if (encounter.status === 'Completed' || encounter.status === 'Cancelled') {
      this.createNextEncounter(encounter.admissionId, encounter.doctorId);
      return;
    }
    this.pendingAddNew = true;
    if (this.formRef) {
      this.formRef.sign();
      return;
    }
    // Never open a second encounter while the current one is still unsigned.
    this.pendingAddNew = false;
    this.toast.error('The clerking form is still loading. Try again in a moment.');
  }

  private createNextEncounter(admissionId: string, doctorId: string): void {
    const encounter = this.encounter();
    if (!encounter) return;
    this.busy.set(true);
    const medications = (encounter.prescriptions ?? [])
      .flatMap((rx) => (rx.items ?? []).map((item) => ({
        stockItemId: item.stockItemId,
        dose: item.dose,
        route: item.route,
        frequency: item.frequency,
        durationDays: item.durationDays,
        quantity: item.quantityPrescribed,
        instructions: item.instructions,
      })));
    this.api.clinical.addInpatientEncounter(admissionId, {
      doctorId,
      presentingComplaint: encounter.presentingComplaint,
      historyOfPresentingComplaint: encounter.historyOfPresentingComplaint,
      pastMedicalSurgicalHistory: encounter.pastMedicalSurgicalHistory,
      familyHistory: encounter.familyHistory,
      diagnosis: encounter.diagnosis,
      clerkingDetails: encounter.clerking ? JSON.stringify({
        generalObservation: encounter.clerking.generalObservation,
        pointOfCare: encounter.clerking.pointOfCare,
        clinicalImpression: encounter.clerking.clinicalImpression,
        planAdvice: encounter.clerking.planAdvice,
        followUpDate: encounter.clerking.followUpDate,
        referralDetails: encounter.clerking.referralDetails,
        rosNotes: encounter.clerking.rosNotes,
        allergies: encounter.clerking.allergies,
      }) : null,
      medications: medications.length ? medications : null,
    }).subscribe({
      next: (created) => {
        this.busy.set(false);
        this.notice.set(`Encounter ${created.encounterNumber} added.`);
        this.load(created.id);
        void this.router.navigate(['/doctor/encounters', created.id]);
      },
      error: () => this.busy.set(false),
    });
  }

  private persist(payload: SaveEncounterRequest, message: string, onSaved?: () => void): void {
    const encounter = this.encounter();
    if (!encounter) return;
    this.busy.set(true);
    this.api.clinical.saveEncounter(encounter.id, payload).subscribe({
      next: (updated) => {
        this.busy.set(false);
        this.encounter.set(updated);
        this.notice.set(message);
        this.toast.success(message);
        onSaved?.();
      },
      error: () => this.busy.set(false),
    });
  }

  /** Safely reads a legacy clerking field from a prior encounter (backward compatibility). */
  protected clerkingField(clerking: any, field: string): string | null {
    return clerking?.[field] ?? null;
  }
}

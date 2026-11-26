import { AbstractControl, FormArray, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Component, computed, effect, inject, input, output, signal, untracked, WritableSignal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { StockSelectComponent } from './stock-select.component';
import { debounce } from '../../core/debounce';
import { StockItem } from '../../core/models';
import {
  CatalogOption, ClerkingContext, ClerkingOrder, ClerkingPrescription, Disposition, DURATION_UNITS, DISPOSITIONS,
  DISPOSITION_LABELS, EXAM_SYSTEMS, FREQUENCY_DOSES_PER_DAY, MEDICATION_FREQUENCIES, MEDICATION_ROUTES,
  MedicationFrequency, DurationUnit, ORDER_PRIORITIES, PROCEDURE_OPTIONS, ROS_SYSTEMS, deriveMedicationQuantity, SaveEncounterRequest,
} from './clerking.models';

type SectionStatus = 'Complete' | 'Incomplete' | 'Pending';

function toCatalogOption(item: StockItem): CatalogOption {
  const privatePrice = item.prices.find((price) => price.fundingSource === 'Private');
  return { id: item.id, code: item.code, name: item.name, detail: privatePrice ? `₦${privatePrice.unitPrice.toLocaleString()}` : null };
}

/** Group validator: a system is either flagged normal or must have free-text findings. */
function examFindingValidator(control: AbstractControl): ValidationErrors | null {
  const normal = control.get('normal')?.value === true;
  const findings = (control.get('findings')?.value ?? '').toString().trim();
  return !normal && !findings ? { findingsRequired: true } : null;
}

/** Group validator: conditional requirements for Follow-up and Referral dispositions. */
function dispositionValidator(control: AbstractControl): ValidationErrors | null {
  const disposition = control.get('disposition')?.value;
  if (disposition === 'FollowUp' && !control.get('followUpDate')?.value) return { followUpDateRequired: true };
  if (disposition === 'ReferToSpecialist' && !(control.get('referralDetails')?.value ?? '').toString().trim())
    return { referralDetailsRequired: true };
  return null;
}

/**
 * Structured SOAP clerking form. Every required field is validated before signing; Save Draft bypasses
 * validation and emits the same structured payload. The form emits structured data only — persistence
 * is the doctor workspace's responsibility.
 */
@Component({
  selector: 'app-clerking-form',
  imports: [ReactiveFormsModule, StockSelectComponent],
  templateUrl: './clerking-form.component.html',
})
export class ClerkingFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);

  readonly context = input.required<ClerkingContext>();

  readonly draftSaved = output<SaveEncounterRequest>();
  readonly signed = output<SaveEncounterRequest>();
  readonly validationFailed = output<void>();

  // Fixed vocabularies
  readonly rosSystems = ROS_SYSTEMS;
  readonly examinationSystems = EXAM_SYSTEMS;
  readonly medicationRoutes = MEDICATION_ROUTES;
  readonly medicationFrequencies = MEDICATION_FREQUENCIES;
  readonly durationUnits = DURATION_UNITS;
  readonly orderPriorities = ORDER_PRIORITIES;
  readonly dispositions = DISPOSITIONS;
  readonly dispositionOptions = DISPOSITIONS.map((value) => ({ value, label: DISPOSITION_LABELS[value] }));
  readonly frequencyDosesPerDay = FREQUENCY_DOSES_PER_DAY;

  // Search-as-you-type results (the full catalogue is never loaded on open).
  readonly medications = signal<CatalogOption[]>([]);
  readonly investigations = signal<CatalogOption[]>([]);
  readonly procedureResults = signal<CatalogOption[]>([]);
  readonly medicationLoading = signal(false);
  readonly investigationLoading = signal(false);
  readonly procedureLoading = signal(false);
  readonly medicationSearched = signal(false);
  readonly investigationSearched = signal(false);
  readonly procedureSearched = signal(false);

  // UI state
  readonly activeSection = signal<'A' | 'B' | 'C' | 'D'>('A');
  readonly locked = signal(false);
  readonly showErrors = signal(false);
  readonly validationMessage = signal<string | null>(null);
  readonly allergyInput = signal('');
  readonly medicationQueries = signal<Record<number, string>>({});
  readonly orderQueries = signal<Record<number, string>>({});
  readonly procedureQueries = signal<Record<number, string>>({});

  readonly form = this.fb.group({
    subjective: this.fb.group({
      presentingComplaint: ['', [Validators.required, Validators.maxLength(1000)]],
      hpi: ['', Validators.maxLength(1000)],
      rosNotes: [''],
      pastMedicalSurgicalHistory: [''],
      familyHistory: [''],
      allergies: this.fb.array<string>([]),
    }),
    objective: this.fb.group({
      generalObservation: ['', Validators.required],
      pointOfCare: this.fb.array<FormGroup>([]),
    }),
    assessment: this.fb.group({
      primaryDiagnosis: ['', Validators.required],
      clinicalImpression: ['', Validators.maxLength(1000)],
    }),
    plan: this.fb.group({
      prescriptions: this.fb.array<FormGroup>([]),
      orders: this.fb.array<FormGroup>([]),
      procedureRequests: this.fb.array<FormGroup>([]),
      procedures: this.fb.array<string>([]),
      advice: [''],
      disposition: ['' as string, Validators.required],
      followUpDate: [''],
      referralDetails: [''],
    }, { validators: dispositionValidator }),
  });

  private readonly formVersion = signal(0);

  readonly sections: Array<{ key: 'A' | 'B' | 'C' | 'D'; label: string }> = [
    { key: 'A', label: 'Subjective' },
    { key: 'B', label: 'Objective' },
    { key: 'C', label: 'Assessment' },
    { key: 'D', label: 'Plan' },
  ];

  readonly sectionStatus = computed<Record<'A' | 'B' | 'C' | 'D', SectionStatus>>(() => {
    this.formVersion();
    return {
      A: this.statusOf(this.form.controls.subjective),
      B: this.statusOf(this.form.controls.objective),
      C: this.statusOf(this.form.controls.assessment),
      D: this.statusOf(this.form.controls.plan),
    };
  });

  readonly completedCount = computed(() =>
    Object.values(this.sectionStatus()).filter((status) => status === 'Complete').length);

  constructor() {
    effect(() => {
      const context = this.context();
      untracked(() => this.applyContext(context));
    });
    this.form.valueChanges.subscribe(() => this.formVersion.update((value) => value + 1));
  }

  private readonly runMedicationSearch = debounce((term: string) =>
    this.fetchStock('Medication', term, this.medications, this.medicationLoading, this.medicationSearched), 300);
  private readonly runInvestigationSearch = debounce((term: string) =>
    this.fetchStock('Test', term, this.investigations, this.investigationLoading, this.investigationSearched), 300);
  private readonly runProcedureSearch = debounce((term: string) =>
    this.fetchStock('Procedure', term, this.procedureResults, this.procedureLoading, this.procedureSearched), 300);

  onMedicationSearch(term: string): void { this.runMedicationSearch(term); }
  onInvestigationSearch(term: string): void { this.runInvestigationSearch(term); }
  onProcedureSearch(term: string): void { this.runProcedureSearch(term); }

  private fetchStock(kind: string, term: string, target: WritableSignal<CatalogOption[]>,
    loading: WritableSignal<boolean>, searched: WritableSignal<boolean>): void
  {
    const query = term.trim();
    if (query.length < 2) {
      target.set([]);
      loading.set(false);
      searched.set(false);
      return;
    }
    loading.set(true);
    searched.set(true);
    this.api.reference.stockItems(kind, null, query).subscribe({
      next: (items) => { target.set(items.map(toCatalogOption)); loading.set(false); },
      error: () => { target.set([]); loading.set(false); },
    });
  }

  // ---- FormArray accessors -------------------------------------------------
  get allergies(): FormArray { return this.form.controls.subjective.controls.allergies; }
  get pointOfCare(): FormArray<FormGroup> { return this.form.controls.objective.controls.pointOfCare; }
  get prescriptions(): FormArray<FormGroup> { return this.form.controls.plan.controls.prescriptions; }
  get orders(): FormArray<FormGroup> { return this.form.controls.plan.controls.orders; }
  get procedureRequests(): FormArray<FormGroup> { return this.form.controls.plan.controls.procedureRequests; }
  get procedures(): FormArray { return this.form.controls.plan.controls.procedures; }

  private newPointOfCare(): FormGroup {
    return this.fb.group({ key: ['', Validators.required], value: ['', Validators.required] });
  }

  private newPrescription(): FormGroup {
    return this.fb.group({
      id: [''],
      medicationId: ['', Validators.required],
      medicationName: [''],
      dosage: ['', Validators.required],
      route: ['' as string, Validators.required],
      frequency: ['' as string, Validators.required],
      durationValue: [null as number | null, [Validators.required, Validators.min(1)]],
      durationUnit: ['Days' as string, Validators.required],
      // Quantity is derived from frequency × duration, not entered manually.
      quantity: [{ value: 1, disabled: true }, Validators.required],
      instructions: [''],
    });
  }

  private newPrescriptionWithDerivedQuantity(): FormGroup {
    const group = this.newPrescription();
    // Re-derive quantity whenever frequency or duration changes.
    group.get('frequency')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
    group.get('durationValue')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
    group.get('durationUnit')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
    return group;
  }

  private deriveQuantityForRow(group: FormGroup): void {
    const frequency = group.get('frequency')?.value as MedicationFrequency | '';
    const durationValue = group.get('durationValue')?.value as number | null;
    const durationUnit = group.get('durationUnit')?.value as DurationUnit;
    const quantity = deriveMedicationQuantity(frequency, durationValue, durationUnit);
    group.get('quantity')?.setValue(quantity, { emitEvent: false });
  }

  private newOrder(): FormGroup {
    return this.fb.group({
      id: [''],
      testId: ['', Validators.required],
      testName: [''],
      priority: ['Routine' as string, Validators.required],
      clinicalNote: [''],
    });
  }

  private orderRow(value: ClerkingOrder): FormGroup {
    return this.fb.group({
      id: [value.id ?? ''],
      testId: [value.testId, Validators.required],
      testName: [value.testName],
      priority: [value.priority ?? 'Routine', Validators.required],
      clinicalNote: [value.clinicalNote ?? ''],
    });
  }

  private queryMap<T>(rows: T[] | null | undefined, label: (row: T) => string): Record<number, string> {
    const map: Record<number, string> = {};
    (rows ?? []).forEach((row, index) => { map[index] = label(row); });
    return map;
  }

  private statusOf(control: AbstractControl): SectionStatus {
    if (control.valid) return 'Complete';
    return control.pristine ? 'Pending' : 'Incomplete';
  }

  private applyContext(context: ClerkingContext): void {
    this.locked.set(context.locked);
    if (context.locked) this.form.disable({ emitEvent: false });
    else this.form.enable({ emitEvent: false });

    this.form.patchValue({
      subjective: {
        presentingComplaint: context.presentingComplaint ?? '',
        hpi: context.historyOfPresentingComplaint ?? '',
        rosNotes: context.rosNotes ?? '',
        pastMedicalSurgicalHistory: context.pastMedicalSurgicalHistory ?? '',
        familyHistory: context.familyHistory ?? '',
      },
      objective: {
        generalObservation: context.generalObservation ?? '',
      },
      assessment: {
        primaryDiagnosis: context.diagnosis ?? '',
        clinicalImpression: context.clinicalImpression ?? '',
      },
      plan: {
        disposition: context.disposition ?? '',
        advice: context.planAdvice ?? '',
        followUpDate: context.followUpDate ?? '',
        referralDetails: context.referralDetails ?? '',
      },
    }, { emitEvent: false });

    this.replaceArray(this.allergies, context.allergies ?? [], (value) => this.fb.control(value));
    this.replaceArray(this.pointOfCare, context.pointOfCare ?? [],
      (value) => this.fb.group({ key: [value.key], value: [value.value ?? ''] }));

    // Draft plan items are restored into the form so they can be edited and re-saved without duplicating.
    this.replaceArray(this.prescriptions, context.prescriptions ?? [],
      (value: ClerkingPrescription) => {
        const group = this.fb.group({
          id: [value.id ?? ''],
          medicationId: [value.medicationId, Validators.required],
          medicationName: [value.medicationName],
          dosage: [value.dosage, Validators.required],
          route: [value.route, Validators.required],
          frequency: [value.frequency, Validators.required],
          durationValue: [value.durationValue, [Validators.required, Validators.min(1)]],
          durationUnit: [value.durationUnit ?? 'Days', Validators.required],
          quantity: [{ value: value.quantity ?? 1, disabled: true }, Validators.required],
          instructions: [value.instructions ?? ''],
        });
        group.get('frequency')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
        group.get('durationValue')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
        group.get('durationUnit')?.valueChanges.subscribe(() => this.deriveQuantityForRow(group));
        this.deriveQuantityForRow(group);
        return group;
      });
    this.replaceArray(this.orders, context.orders ?? [], (value: ClerkingOrder) => this.orderRow(value));
    this.replaceArray(this.procedureRequests, context.procedureRequests ?? [], (value: ClerkingOrder) => this.orderRow(value));

    this.medicationQueries.set(this.queryMap(context.prescriptions, (row) => row.medicationName));
    this.orderQueries.set(this.queryMap(context.orders, (row) => row.testName));
    this.procedureQueries.set(this.queryMap(context.procedureRequests, (row) => row.testName));
  }

  private replaceArray(array: FormArray, values: unknown[], factory: (value: any) => AbstractControl): void {
    array.clear({ emitEvent: false });
    for (const value of values) array.push(factory(value), { emitEvent: false });
  }

  // ---- Searchable select helpers ------------------------------------------
  filteredMedications(index: number): CatalogOption[] {
    const query = this.medicationQueries()[index] ?? '';
    const term = query.toLowerCase().trim();
    return this.medications().filter((option) => !term || option.name.toLowerCase().includes(term) || option.code.toLowerCase().includes(term)).slice(0, 12);
  }

  filteredInvestigations(index: number): CatalogOption[] {
    const query = this.orderQueries()[index] ?? '';
    const term = query.toLowerCase().trim();
    return this.investigations().filter((option) => !term || option.name.toLowerCase().includes(term) || option.code.toLowerCase().includes(term)).slice(0, 12);
  }

  filteredProcedures(index: number): CatalogOption[] {
    const query = this.procedureQueries()[index] ?? '';
    const term = query.toLowerCase().trim();
    return this.procedureResults().filter((option) => !term || option.name.toLowerCase().includes(term) || option.code.toLowerCase().includes(term)).slice(0, 12);
  }

  onMedicationQuery(index: number, term: string): void {
    this.medicationQueries.update((map) => ({ ...map, [index]: term }));
    this.onMedicationSearch(term);
  }

  onOrderQuery(index: number, term: string): void {
    this.orderQueries.update((map) => ({ ...map, [index]: term }));
    this.onInvestigationSearch(term);
  }

  onProcedureQuery(index: number, term: string): void {
    this.procedureQueries.update((map) => ({ ...map, [index]: term }));
    this.onProcedureSearch(term);
  }

  /** Same field population the datalist performed: code · name for display, id for the payload. */
  onMedicationSelected(index: number, option: CatalogOption): void {
    this.medicationQueries.update((map) => ({ ...map, [index]: option.code }));
    this.prescriptions.at(index).patchValue({
      medicationId: option.id,
      medicationName: `${option.code} · ${option.name}`,
    });
  }

  onOrderSelected(index: number, option: CatalogOption): void {
    this.orderQueries.update((map) => ({ ...map, [index]: option.code }));
    this.orders.at(index).patchValue({
      testId: option.id,
      testName: `${option.code} · ${option.name}`,
    });
  }

  onProcedureSelected(index: number, option: CatalogOption): void {
    this.procedureQueries.update((map) => ({ ...map, [index]: option.code }));
    this.procedureRequests.at(index).patchValue({
      testId: option.id,
      testName: `${option.code} · ${option.name}`,
    });
  }

  medicationQueryId(index: number): string { return `medication-options-${index}`; }
  investigationQueryId(index: number): string { return `investigation-options-${index}`; }

  // ---- Repeatable rows and chips ------------------------------------------
  addPrescription(): void { this.prescriptions.push(this.newPrescriptionWithDerivedQuantity()); }
  removePrescription(index: number): void { this.prescriptions.removeAt(index); delete this.medicationQueries()[index]; }

  addOrder(): void { this.orders.push(this.newOrder()); }
  removeOrder(index: number): void { this.orders.removeAt(index); delete this.orderQueries()[index]; }

  addProcedureRequest(): void { this.procedureRequests.push(this.newOrder()); }
  removeProcedureRequest(index: number): void {
    this.procedureRequests.removeAt(index);
    delete this.procedureQueries()[index];
  }
  addPointOfCare(): void { this.pointOfCare.push(this.newPointOfCare()); }
  removePointOfCare(index: number): void { this.pointOfCare.removeAt(index); }

  addAllergy(): void {
    const value = this.allergyInput().trim();
    if (!value || (this.allergies.value as string[]).includes(value)) { this.allergyInput.set(''); return; }
    this.allergies.push(this.fb.control(value));
    this.allergyInput.set('');
  }
  removeAllergy(index: number): void { this.allergies.removeAt(index); }

  toggleProcedure(name: string): void {
    const current = this.procedures.value as string[];
    if (current.includes(name)) this.procedures.removeAt(current.indexOf(name));
    else this.procedures.push(name);
  }
  isProcedureSelected(name: string): boolean { return (this.procedures.value as string[]).includes(name); }

  // ---- Section navigation --------------------------------------------------
  selectSection(section: 'A' | 'B' | 'C' | 'D'): void { this.activeSection.set(section); }

  // ---- Actions -------------------------------------------------------------
  saveDraft(): void {
    if (this.locked()) return;
    const incomplete = this.incompletePlanRows();
    if (incomplete.length) {
      this.validationMessage.set(
        `Complete the highlighted fields before saving the draft: ${incomplete.join(', ')}.`);
      this.showErrors.set(true);
      this.activeSection.set('D');
      this.validationFailed.emit();
      return;
    }
    this.validationMessage.set(null);
    this.draftSaved.emit(this.buildSaveRequest(false));
  }

  sign(): void {
    if (this.locked()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.validationMessage.set('Complete every required field before signing. The first incomplete section has been opened.');
      this.showErrors.set(true);
      this.validationFailed.emit();

      const errors = this.collectErrors(this.form);
      console.table(errors);

      const statuses = this.sectionStatus();
      const firstInvalid = (['A', 'B', 'C', 'D'] as const).find((key) => statuses[key] !== 'Complete');
      if (firstInvalid) this.activeSection.set(firstInvalid);
      return;
    }
    this.locked.set(true);
    this.form.disable({ emitEvent: false });
    this.signed.emit(this.buildSaveRequest(true));
  }

  /** Draft saves only require rows that were actually started to be complete. */
  private incompletePlanRows(): string[] {
    const filled = (value: unknown) =>
      value !== null && value !== undefined && String(value).trim() !== '';
    const problems: string[] = [];
    const check = (rows: FormArray, touched: (row: FormGroup) => boolean, label: string) => {
      rows.controls.forEach((control, index) => {
        const row = control as FormGroup;
        if (!touched(row)) return;
        row.markAllAsTouched();
        if (row.invalid) problems.push(`${label} row ${index + 1}`);
      });
    };

    check(this.prescriptions, (row) =>
      filled(row.get('medicationId')?.value)
      || ['dosage', 'route', 'frequency', 'durationValue', 'instructions'].some((key) => filled(row.get(key)?.value)),
      'medication');
    check(this.orders, (row) =>
      filled(row.get('testId')?.value) || filled(row.get('clinicalNote')?.value), 'test');
    check(this.procedureRequests, (row) =>
      filled(row.get('testId')?.value) || filled(row.get('clinicalNote')?.value), 'procedure');

    return problems;
  }

  private buildSaveRequest(complete: boolean): SaveEncounterRequest {
    const raw: any = this.form.getRawValue();
    const disposition = raw.plan.disposition as Disposition | '';
    return {
      presentingComplaint: (raw.subjective.presentingComplaint ?? '').toString().trim() || null,
      historyOfPresentingComplaint: (raw.subjective.hpi ?? '').toString().trim() || null,
      pastMedicalSurgicalHistory: (raw.subjective.pastMedicalSurgicalHistory ?? '').toString().trim() || null,
      familyHistory: (raw.subjective.familyHistory ?? '').toString().trim() || null,
      diagnosis: (raw.assessment.primaryDiagnosis ?? '').toString().trim() || null,
      disposition: disposition || null,
      examination: null,
      complete,
      medications: (raw.plan.prescriptions ?? []).filter((row: any) => row.medicationId)
        .map((row: any) => {
          const durationValue = row.durationValue ?? 1;
          let durationDays = durationValue;
          if (row.durationUnit === 'Weeks') durationDays = durationValue * 7;
          if (row.durationUnit === 'Months') durationDays = durationValue * 30;
          return {
            id: row.id || null,
            stockItemId: row.medicationId,
            dose: row.dosage,
            route: row.route,
            frequency: row.frequency,
            durationDays,
            quantity: deriveMedicationQuantity(row.frequency, row.durationValue, row.durationUnit),
            instructions: row.instructions || null,
          };
        }),
      tests: (raw.plan.orders ?? []).filter((row: any) => row.testId)
        .map((row: any) => ({ id: row.id || null, stockItemId: row.testId, priority: row.priority, clinicalNote: row.clinicalNote })),
      procedures: (raw.plan.procedureRequests ?? []).filter((row: any) => row.testId)
        .map((row: any) => ({ id: row.id || null, stockItemId: row.testId, priority: row.priority, clinicalNote: row.clinicalNote })),
      clerking: {
        rosNotes: this.textOrNull(raw.subjective.rosNotes),
        allergies: (raw.subjective.allergies ?? []).filter((value: unknown) => !!value),
        generalObservation: this.textOrNull(raw.objective.generalObservation),
        pointOfCare: (raw.objective.pointOfCare ?? []).filter((row: any) => row.key)
          .map((row: any) => ({ key: row.key, value: this.textOrNull(row.value) })),
        clinicalImpression: this.textOrNull(raw.assessment.clinicalImpression),
        planAdvice: this.textOrNull(raw.plan.advice),
        followUpDate: this.textOrNull(raw.plan.followUpDate),
        referralDetails: this.textOrNull(raw.plan.referralDetails),
      },
    };
  }

  private textOrNull(value: unknown): string | null {
    const text = value === null || value === undefined ? '' : value.toString().trim();
    return text || null;
  }

  private collectErrors(
    control: AbstractControl,
    path = '',
  ): Array<{ field: string; errors: ValidationErrors }> {
    const results: Array<{ field: string; errors: ValidationErrors }> = [];

    if (control.errors) {
      results.push({ field: path || 'form', errors: control.errors });
    }

    if (control instanceof FormGroup) {
      for (const [key, child] of Object.entries(control.controls)) {
        const nextPath = path ? `${path}.${key}` : key;
        results.push(...this.collectErrors(child, nextPath));
      }
    } else if (control instanceof FormArray) {
      control.controls.forEach((child, index) => {
        results.push(...this.collectErrors(child, `${path}[${index}]`));
      });
    }

    return results;
  }
}

import { Patient } from '../../core/models';

export type MedicationRoute = 'Oral' | 'IV' | 'IM' | 'Topical' | 'Inhaled';
export type MedicationFrequency = 'OD' | 'BD' | 'TDS' | 'QID' | 'PRN';
export type DurationUnit = 'Days' | 'Weeks' | 'Months';
export type OrderPriority = 'Routine' | 'Urgent';
export type Disposition = 'Discharge' | 'FollowUp' | 'Admit' | 'ReferToSpecialist';
export type SectionKey = 'A' | 'B' | 'C' | 'D';

export interface KeyValueEntry { key: string; value: string | null; }
export interface ExaminationItem { name: string; result: string | null; normal?: boolean | null; }

/** Doses per day for each administration frequency. PRN (as needed) defaults to one dose per day. */
export const FREQUENCY_DOSES_PER_DAY: Record<MedicationFrequency, number> = {
  OD: 1, BD: 2, TDS: 3, QID: 4, PRN: 1,
};

/** Derive the total medication quantity from frequency and duration (doses/day × days). */
export function deriveMedicationQuantity(
  frequency: MedicationFrequency | '',
  durationValue: number | null,
  durationUnit: DurationUnit,
): number {
  const dosesPerDay = FREQUENCY_DOSES_PER_DAY[frequency as MedicationFrequency] ?? 1;
  const days = durationValue ?? 1;
  let totalDays = days;
  if (durationUnit === 'Weeks') totalDays = days * 7;
  if (durationUnit === 'Months') totalDays = days * 30;
  return Math.max(1, Math.round(totalDays * dosesPerDay));
}

/** Section A — subjective history and complaints. */
export interface ClerkingSubjective {
  presentingComplaint: string;
  hpi: string;
  ros: Record<string, boolean>;
  rosNotes: string;
  pastMedicalSurgicalHistory: string;
  familyHistory: string;
  allergies: string[];
}

/** Section B — objective examination findings. */
export interface ClerkingObjective {
  generalObservation: string;
  pointOfCare: KeyValueEntry[];
}

/** Section C — assessment / diagnosis. */
export interface ClerkingAssessment {
  primaryDiagnosis: string;
  clinicalImpression: string;
}

/** Section D — plan, orders, prescriptions and disposition. */
export interface ClerkingPrescription {
  id: string | null;
  medicationId: string; medicationName: string; dosage: string;
  route: MedicationRoute | ''; frequency: MedicationFrequency | '';
  durationValue: number | null; durationUnit: DurationUnit;
  /** Derived from frequency × duration, not entered manually. */
  quantity: number;
  instructions: string;
}
export interface ClerkingOrder {
  id: string | null;
  testId: string; testName: string; priority: OrderPriority; clinicalNote: string;
}
export interface ClerkingPlan {
  prescriptions: ClerkingPrescription[];
  orders: ClerkingOrder[];
  procedures: string[];
  advice: string;
  disposition: Disposition | '';
  followUpDate: string;
  referralDetails: string;
}

/** Structured payload sent to the doctor's encounter save endpoint. */
export interface SaveEncounterRequest {
  presentingComplaint: string | null;
  historyOfPresentingComplaint: string | null;
  pastMedicalSurgicalHistory: string | null;
  familyHistory: string | null;
  diagnosis: string | null;
  disposition: Disposition | null;
  examination: ExaminationItem[] | null;
  complete: boolean;
  medications?: Array<{ id?: string | null; stockItemId: string; dose: string; route: string; frequency: string; durationDays: number; quantity: number; instructions?: string | null }>;
  tests?: Array<{ id?: string | null; stockItemId: string; priority: string | null; clinicalNote: string | null }>;
  procedures?: Array<{ id?: string | null; stockItemId: string; priority: string | null; clinicalNote: string | null }>;
  clerking?: {
    rosNotes: string | null;
    allergies: string[] | null;
    generalObservation: string | null;
    pointOfCare: Array<{ key: string; value: string | null }> | null;
    clinicalImpression: string | null;
    planAdvice: string | null;
    followUpDate: string | null;
    referralDetails: string | null;
  } | null;
}

/** Read-only context supplied by the doctor workspace for prefilling the form. */
export interface ClerkingContext {
  encounterId: string;
  encounterNumber: string;
  encounterType: string;
  patient: Patient;
  presentingComplaint: string | null;
  historyOfPresentingComplaint: string | null;
  pastMedicalSurgicalHistory: string | null;
  familyHistory: string | null;
  generalObservation: string | null;
  diagnosis: string | null;
  clinicalImpression: string | null;
  disposition: Disposition | null;
  examination: ExaminationItem[] | null;
  rosNotes: string | null;
  allergies: string[] | null;
  pointOfCare: KeyValueEntry[] | null;
  planAdvice: string | null;
  followUpDate: string | null;
  referralDetails: string | null;
  prescriptions: ClerkingPrescription[];
  orders: ClerkingOrder[];
  procedureRequests: ClerkingOrder[];
  locked: boolean;
}

export interface CatalogOption { id: string; code: string; name: string; detail: string | null; }

export const ROS_SYSTEMS = [
  'Respiratory', 'Cardiovascular', 'GI', 'Neurological',
  'Musculoskeletal', 'Genitourinary', 'Skin',
] as const;

export const EXAM_SYSTEMS = [
  'HEENT', 'Chest/Lungs', 'Cardiovascular', 'Abdomen', 'CNS/Neuro', 'Skin',
] as const;

export const MEDICATION_ROUTES: MedicationRoute[] = ['Oral', 'IV', 'IM', 'Topical', 'Inhaled'];
export const MEDICATION_FREQUENCIES: MedicationFrequency[] = ['OD', 'BD', 'TDS', 'QID', 'PRN'];
export const DURATION_UNITS: DurationUnit[] = ['Days', 'Weeks', 'Months'];
export const ORDER_PRIORITIES: OrderPriority[] = ['Routine', 'Urgent'];
export const DISPOSITIONS: Disposition[] = ['Discharge', 'FollowUp', 'Admit', 'ReferToSpecialist'];
export const DISPOSITION_LABELS: Record<Disposition, string> = {
  Discharge: 'Discharge (close treatment & start billing)',
  FollowUp: 'Follow-up',
  Admit: 'Admit (outpatient → inpatient)',
  ReferToSpecialist: 'Refer to Specialist',
};

/** Fallback procedure list, extended by the backend procedure catalogue when available. */
export const PROCEDURE_OPTIONS = [
  'Wound dressing', 'Nebulisation', 'IV cannulation', 'Catheterisation',
  'Suture removal', 'Nasal packing', 'Arterial puncture', 'Lumbar puncture',
];

export function emptySaveRequest(): SaveEncounterRequest {
  return {
    presentingComplaint: null,
    historyOfPresentingComplaint: null,
    pastMedicalSurgicalHistory: null,
    familyHistory: null,
    diagnosis: null,
    disposition: null,
    examination: null,
    complete: false,
  };
}

export function optionLabel(option: CatalogOption): string {
  return option.detail ? `${option.code} · ${option.name} (${option.detail})` : `${option.code} · ${option.name}`;
}

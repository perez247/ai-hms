// Shared API contracts. Enum-like values are strings because the API serializes .NET enums as names.
export type StaffRole =
  | 'Receptionist' | 'Nurse' | 'Doctor' | 'Pharmacy'
  | 'Finance' | 'HmoOffice' | 'Admin' | 'HrOffice' | 'Lab';

export interface MeResponse {
  id: string;
  fullName: string;
  role: StaffRole;
  roleName: string;
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: MeResponse;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface HmoOption { id: string; code: string; name: string; }
export interface HmoTariffItem {
  id: string; code: string; description: string; category: string; unit: string | null;
  unitPrice: number; coveragePercent: number; requiresPriorAuthorization: boolean; isActive: boolean;
}
export interface HmoTariff {
  id: string; hmoCompanyId: string; name: string; version: number; effectiveFrom: string;
  effectiveTo: string | null; status: string; items: HmoTariffItem[];
}
export interface HmoDirectoryEntry {
  id: string; code: string; name: string; isActive: boolean; tariffs: HmoTariff[];
}
export interface MedicationPriceOption {
  payerType: string; hmoCompanyId: string | null; label: string; totalPrice: number;
  hmoCoveredAmount: number; patientAmount: number; undercovered: boolean; warnings: string[];
}
export interface PrescriptionPriceOptions {
  encounterId: string; marketTotal: number; options: MedicationPriceOption[];
}
export interface ManualBillResult { bill: Bill; warnings: string[]; }
export interface DoctorQueuePatient {
  sourceId: string; patientId: string; patientNumber: string; patientName: string;
  referenceNumber: string; sourceKind: string; since: string; hmoName: string | null;
  hasOpenEncounter: boolean; openEncounterId: string | null;
}
export interface DoctorQueue { awaitingOutpatients: DoctorQueuePatient[]; activeInpatients: DoctorQueuePatient[]; }
export interface EncounterListItem {
  id: string; encounterNumber: string; patientId: string; patientNumber: string; patientName: string;
  doctorName: string; type: string; status: string; isEmergency: boolean; startedAt: string;
  appointmentId: string | null; appointmentNumber: string | null;
  admissionId: string | null; admissionNumber: string | null; presentingComplaint: string | null;
}
export interface ReferenceOption { id: string; code: string; name: string; detail: string | null; }
export interface DoctorOption { id: string; fullName: string; }
export interface HmoEnrollmentOption {
  id: string; hmoCompanyId: string; hmoName: string; hmoMemberId: string; planName: string | null; isPrimary: boolean;
}

export interface PatientHmoEnrollment {
  id: string; hmoCompanyId: string; hmoName: string; hmoMemberId: string;
  planName: string | null; validFrom: string; validTo: string | null; isPrimary: boolean; isActive: boolean;
}

export interface Patient {
  id: string; patientNumber: string; fullName: string; firstName: string; lastName: string; middleName: string | null; sex: string; dateOfBirth: string;
  phone: string | null; email: string | null; address: string | null;
  stateOfOrigin: string | null; homeTown: string | null; allergy: string | null; occupation: string | null;
  nextOfKinName: string | null; nextOfKinPhone: string | null;
  registrationDate: string; currentRegistrationExpiresAt: string; status: string;
  hmoEnrollments: PatientHmoEnrollment[];
}

export interface PatientDetails {
  canUploadFiles: boolean; isPrivate: boolean;
  patient: Patient;
}

export interface PatientSearchItem {
  id: string; patientNumber: string; fullName: string; phone: string | null;
  currentRegistrationExpiresAt: string; status: string;
}

export interface CreatePatientRequest {
  firstName: string; middleName: string | null; lastName: string; sex: string; dateOfBirth: string;
  phone: string | null; email: string | null; address: string | null;
  stateOfOrigin: string | null; homeTown: string | null; allergy: string | null; occupation: string | null;
  nextOfKinName: string | null; nextOfKinPhone: string | null;
  hmoEnrollments: Array<{
    hmoCompanyId: string; hmoMemberId: string; planName: string | null;
    validFrom: string; validTo: string | null; isPrimary: boolean;
  }>;
}

export interface UpdatePatientRequest {
  firstName: string; middleName: string | null; lastName: string; sex: string; dateOfBirth: string;
  phone: string | null; email: string | null; address: string | null;
  stateOfOrigin: string | null; homeTown: string | null; allergy: string | null; occupation: string | null;
  nextOfKinName: string | null; nextOfKinPhone: string | null;
}

export interface PharmacyPrescriptionItem {
  id: string; medicationId: string; medicationName: string; strength: string | null; form: string | null;
  unit: string; stockOnHand: number; quantityPrescribed: number; quantityDispensed: number;
  unitPrice: number; tariffCode: string | null; hmoUnitPrice: number | null;
  hmoCoveragePercent: number | null; isInpatient: boolean;
}

export interface PharmacyQueueItem {
  encounterId: string; encounterNumber: string; patientName: string; isInpatient: boolean;
  prescriptionStatus: string; createdAt: string;
}

export interface PharmacyPrescription {
  id: string; encounterId: string; encounterNumber: string; patientName: string; isInpatient: boolean;
  status: string; notes: string | null; items: PharmacyPrescriptionItem[];
  canCommitToFinance?: boolean; commitBlockedReason?: string | null;
}

export interface DispenseResult {
  dispenseId: string; billId: string; billStatus: string; committedToFinance: boolean;
  patientAmount: number; hmoAmount: number; warnings: string[];
}

export interface BillLine {
  id: string; category: string; itemType: string | null; description: string; quantity: number; unitPrice: number;
  grossAmount: number; discountAmount: number; netAmount: number; hmoAmount: number; patientAmount: number;
  isHmoCovered: boolean; tariffItemId: string | null; tariffConfirmedAt: string | null;
  referralDestination: string | null; encounterId: string | null; admissionId: string | null;
  chargeDate: string; createdBy: string | null;
}

export interface Payment {
  id: string; amount: number; method: string; reference: string | null; receivedAt: string; recordedBy: string | null;
}

export interface BillAdjustment {
  id: string; amount: number; reason: string; status: string; createdAt: string; approvedBy: string | null;
}

export interface HmoReceivable {
  id: string; paCode: string; amount: number; settledAmount: number; status: string; dueDate: string | null;
}

export interface Bill {
  id: string; billNumber: string; kind: string; status: string; patientId: string | null; patientNumber: string | null; patientName: string;
  payerType: string; hmoCompanyId: string | null; hmoCompanyName: string | null;
  priorAuthorizationId: string | null; paCode: string | null; currency: string;
  subtotal: number; discountTotal: number; hmoPayableTotal: number; patientPayableTotal: number;
  paidTotal: number; balance: number; encounterId: string | null; admissionId: string | null;
  appointmentId: string | null; registrationPeriodId: string | null; fitnessCertificateId: string | null;
  createdAt: string; committedAt: string | null; hmoApprovedAt: string | null;
  financeApprovedAt: string | null; paidAt: string | null;
  isDebt: boolean; debtOwner: string | null; debtRecordedAt: string | null;
  debtReason: string | null; debtSettledAt: string | null;
  lines: BillLine[]; payments: Payment[]; adjustments: BillAdjustment[]; receivable: HmoReceivable | null;
}

export interface PriorAuthorization {
  id: string; patientId: string; hmoCompanyId: string; hmoName: string;
  encounterId: string | null; admissionId: string | null; paCode: string;
  status: string; requestedAt: string; issuedAt: string | null; expiresAt: string | null;
  approvedAmount: number | null; notes: string | null;
}

export interface UserRecord {
  id: string; fullName: string; email: string; staffRole: StaffRole; employeeNumber: string | null;
  phone: string | null; isActive: boolean; salaryAmount: number | null; bankName: string | null;
  bankAccountName: string | null; bankAccountNumber: string | null; hiredOn: string | null;
  deactivatedAt: string | null;
}

export interface Fee {
  id: string; code: string; amount: number; currency: string; effectiveFrom: string; effectiveTo: string | null; isActive: boolean;
}

export interface FitnessInvestigationResult { key: string; value: string; }

export interface FitnessCertificate {
  id: string; certificateNumber: string; patientId: string | null; patientNumber: string | null;
  patientName: string; patientAge: number; personSex: string; phone: string;
  paymentType: string; fitnessDate: string; expiresAt: string | null;
  hmoCompanyId: string | null; hmoCompanyName: string | null; status: string; paymentStatus: string; rejectionReason: string | null;
  certificateFileName: string | null; certificateContentType: string | null;
  submittedAt: string | null; approvedAt: string | null; paymentUpdatedAt: string | null;
  billId: string | null; investigationResults: FitnessInvestigationResult[];
}

export interface PatientVitalsPage {
  page: number; pageSize: number; total: number;
  items: Array<{ id: string; recordedAt: string; temperatureC: number | null; systolic: number | null; diastolic: number | null; pulse: number | null; respiratoryRate: number | null; spo2: number | null; weightKg: number | null; heightCm: number | null; painScore: number | null; notes: string | null }>;
}

export interface PatientEncounterSummary {
  id: string; encounterNumber: string; type: string; status: string; doctorName: string; startedAt: string; presentingComplaint: string | null;
}

export interface PatientInvestigation {
  id: string; name: string; status: string; encounterNumber: string; orderedAt: string;
  performedAt: string | null; reportText: string | null; referralDestination: string | null;
}

export interface PatientSurgery {
  id: string; name: string; status: string; encounterNumber: string;
  scheduledAt: string; performedAt: string | null; reportText: string | null;
}

export interface PatientFinance {
  totalBilled: number; totalPaid: number; balance: number; payerType: string; bills: Bill[] | null;
}

export interface PatientFile {
  id: string; fileName: string; contentType: string; sizeBytes: number; createdAt: string; uploadedBy: string | null;
}

export interface PatientBasicProfile { patient: Patient; isPrivate: boolean; canUploadFiles: boolean; }

export interface PatientProfile {
  patient: Patient; isPrivate: boolean; vitals: PatientVitalsPage | null;
  fitnessRecords: FitnessCertificate[] | null; encounters: PatientEncounterSummary[] | null;
  investigations: PatientInvestigation[] | null; surgeries: PatientSurgery[] | null;
  finance: PatientFinance | null; files: PatientFile[]; canUploadFiles: boolean;
}

export interface StaffProfile {
  id: string; fullName: string; email: string; phone: string | null; role: string; isActive: boolean;
  canViewEmployment: boolean; hiredOn: string | null; employeeNumber: string | null;
  salaryAmount: number | null; bankName: string | null; bankAccountName: string | null; bankAccountNumber: string | null;
}

export interface Encounter {
  id: string; encounterNumber: string; patientId: string; patientNumber: string; patientName: string; doctorId: string;
  doctorName: string; type: string; status: string; isEmergency: boolean; startedAt: string; closedAt: string | null;
  admissionId: string | null; billId: string | null;
  presentingComplaint: string | null; presentingComplaintDurationDays?: number | null;
  historyOfPresentingComplaint: string | null;
  pastMedicalSurgicalHistory: string | null; familyHistory: string | null;
  examination: Array<{ name: string; result: string | null; normal?: boolean | null }> | null;
  diagnosis: string | null; disposition: string | null;
  fundingSource: 'Private' | 'Hmo'; hmoEnrollmentId: string | null;
  vitals: Array<{ id: string; recordedAt: string; temperatureC: number | null; systolic: number | null; diastolic: number | null; pulse: number | null; respiratoryRate: number | null; spo2: number | null; painScore: number | null; weightKg: number | null; heightCm: number | null; notes: string | null }>;
  prescriptions: Array<{ id: string; status: string; createdAt: string; itemCount: number;
    items?: Array<{ id: string; stockItemId: string; medicationName: string; dose: string; route: string; frequency: string;
      durationDays: number; quantityPrescribed: number; quantityDispensed: number; instructions: string | null }> }>;
  investigations: Array<{ id: string; name: string; status: string; orderedAt: string }>;
  surgeries: Array<{ id: string; name: string; status: string; scheduledAt: string }>;
  serviceRequests: ServiceRequestSummary[];
  priorEncounters?: EncounterHistoryItem[];
  clerking?: ClerkingDetails | null;
}

/** A lab/procedure order plus its returned result, if any. */
export interface ServiceRequestSummary {
  id: string; stockItemId: string; requestNumber: string; kind: string; itemName: string;
  status: string; resultStatus: string | null; resultText: string | null; findings: string | null; unitPrice: number;
  abnormalFlag?: boolean | null; performedAt?: string | null; priority?: string | null; clinicalNote?: string | null;
}

export interface ClerkingDetails {
  rosNotes: string | null;
  allergies: string[] | null;
  generalObservation: string | null;
  pointOfCare: Array<{ key: string; value: string | null }> | null;
  clinicalImpression: string | null;
  planAdvice: string | null;
  followUpDate: string | null;
  referralDetails: string | null;
}

export interface EncounterHistoryItem {
  id: string; encounterNumber: string; type: string; status: string; doctorName: string; startedAt: string;
  presentingComplaint: string | null; diagnosis: string | null; disposition: string | null;
  pastMedicalSurgicalHistory: string | null; familyHistory: string | null;
  presentingComplaintDurationDays?: number | null;
  historyOfPresentingComplaint?: string | null;
  examination?: Array<{ name: string; result: string | null; normal?: boolean | null }> | null;
  clerking?: ClerkingDetails | null;
  medications?: Array<{ id: string; medicationName: string; dose: string; route: string; frequency: string;
    durationDays: number; quantityPrescribed: number; quantityDispensed: number; instructions: string | null }> | null;
  orders?: EncounterPlanOrder[];
  resultCount?: number;
}

/** One lab/procedure order in a previous encounter's plan. */
export interface EncounterPlanOrder {
  id: string; itemName: string; kind: string; status: string; priority: string | null; clinicalNote: string | null;
}

export interface PatientMedication {
  id: string; encounterNumber: string; encounterType: string; medicationName: string;
  dose: string; route: string; frequency: string; durationDays: number;
  quantityPrescribed: number; quantityDispensed: number; instructions: string | null;
  status: string; createdAt: string;
}

export interface OngoingAppointment {
  id: string; appointmentNumber: string; scheduledAt: string; status: string; billStatus: string | null;
}

/** Structured clerking payload sent to the doctor's encounter save endpoint. */
export interface SaveEncounterRequest {
  presentingComplaint: string | null;
  historyOfPresentingComplaint: string | null;
  pastMedicalSurgicalHistory: string | null;
  familyHistory: string | null;
  diagnosis: string | null;
  disposition: string | null;
  examination: Array<{ name: string; result: string | null; normal?: boolean | null }> | null;
  complete: boolean;
  medications?: Array<{ id?: string | null; stockItemId: string; dose: string; route: string; frequency: string; durationDays: number; quantity: number; instructions?: string | null }>;
  tests?: Array<{ id?: string | null; stockItemId: string; priority: string | null; clinicalNote: string | null }>;
  procedures?: Array<{ id?: string | null; stockItemId: string; priority: string | null; clinicalNote: string | null }>;
  clerking?: ClerkingDetails | null;
}

export interface AccountBookEntry {
  id: string; entryNumber: string; entryType: string; direction: string; category: string; amount: number;
  currency: string; occurredAt: string; description: string; reference: string | null; source: string | null;
  billId: string | null; paymentId: string | null;
}

export interface AccountBookSummary {
  totalCredits: number; totalDebits: number; balance: number; currency: string;
}

export interface AppointmentPriceQuote {
  reason: string; fundingSource: string; hmoEnrollmentId: string | null; payerName: string;
  unitPrice: number; coveragePercent: number; hmoAmount: number; patientAmount: number; isConfigured: boolean;
}

export interface NursePrescriptionItem {
  prescriptionItemId: string; dispenseItemId: string | null; medicationName: string; dose: string;
  route: string; frequency: string; quantityPrescribed: number; quantityDispensed: number; canAdminister: boolean;
}

export interface NurseEncounter {
  id: string; encounterNumber: string; type: string; status: string; doctorName: string;
  startedAt: string; admissionStatus: string | null; prescriptions: NursePrescriptionItem[];
}

export interface ShiftHandover {
  id: string; nurseId: string; nurseName: string; shiftDate: string; contentHtml: string; createdAt: string;
}

export interface NurseAppointment {
  id: string; appointmentNumber: string; patientId: string; patientNumber: string; patientName: string;
  doctorId: string; doctorName: string; scheduledAt: string; status: string; reason: string | null;
  fundingSource: 'Private' | 'Hmo'; hmoEnrollmentId: string | null;
}

export interface HmoEnrollmentOptionFull {
  id: string; hmoCompanyId: string; hmoName: string; hmoMemberId: string; planName: string | null;
  validFrom: string; validTo: string | null; isPrimary: boolean; isActive: boolean;
}

export interface StockItem {
  id: string; code: string; name: string; itemType: 'Medication' | 'Test' | 'Procedure';
  category: string | null; genericName: string | null; form: string | null; strength: string | null;
  unit: string; tariffCode: string | null; isActive: boolean;
  quantity: number;
  prices: ItemPrice[];
}
export interface ItemPrice {
  id: string; payerId: string; payerName: string; fundingSource: 'Private' | 'Hmo';
  hmoCompanyId: string | null; hmoName: string | null;
  unitPrice: number; coveragePercent: number; requiresPriorAuthorization: boolean;
  effectiveFrom: string; effectiveTo: string | null; isActive: boolean;
}
export interface PayerOption { id: string; name: string; isPrivate: boolean; }
export interface ImportResult {
  totalRows: number; imported: number;
  errors: Array<{ row: number; column: string; message: string }>;
}
export interface ServiceRequestDto {
  id: string; requestNumber: string; encounterId: string; encounterNumber: string;
  patientId: string; patientNumber: string; patientName: string;
  stockItemId: string; itemName: string; kind: 'Test' | 'Procedure';
  status: string; orderedAt: string; doctorName: string;
  fundingSource: 'Private' | 'Hmo'; hmoName: string | null; priority: string | null; clinicalNote: string | null;
  resultId: string | null; resultStatus: 'Pending' | 'Entered' | 'Submitted' | 'Amended' | null;
  resultText: string | null; findings: string | null; abnormalFlag: boolean | null; version: number;
}
export interface SaveServiceResultRequest {
  resultText: string | null; findings: string | null; structuredJson: string | null;
  abnormalFlag: boolean | null; reason?: string | null;
}
export interface SubmitServiceResultRequest extends SaveServiceResultRequest {
  resultText: string; performedAt?: string | null;
}

export interface NurseInpatient {
  admissionId: string; admissionNumber: string; patientId: string; patientNumber: string; patientName: string;
  attendingDoctorName: string; wardType: string; bedNumber: string | null; admittedAt: string;
  status: string; encounterIds: string[];
}

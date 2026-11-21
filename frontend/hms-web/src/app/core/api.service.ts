import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE } from './api.config';
import {
  AccountBookEntry, Bill, CreatePatientRequest, DispenseResult, DoctorOption, Encounter, Fee,
  FitnessCertificate, HmoEnrollmentOption, HmoOption, PagedResult, Patient, PatientBasicProfile, PatientEncounterSummary, PatientFile, PatientFinance, PatientHmoEnrollment, PatientInvestigation, PatientProfile, PatientSurgery,
  PatientSearchItem, PatientVitalsPage, PharmacyPrescription, PriorAuthorization, StaffProfile, UserRecord,
} from './models';

type Query = Record<string, string | number | boolean | null | undefined>;

/**
 * Typed HTTP facade grouped by backend module. Keeping endpoints here prevents
 * components from scattering URL strings and response types.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  get<T>(path: string, query?: Query) {
    return this.http.get<T>(`${API_BASE}${path}`, { params: this.params(query) });
  }

  post<T>(path: string, body: unknown) {
    return this.http.post<T>(`${API_BASE}${path}`, body);
  }

  put<T>(path: string, body: unknown) {
    return this.http.put<T>(`${API_BASE}${path}`, body);
  }

  delete<T>(path: string) {
    return this.http.delete<T>(`${API_BASE}${path}`);
  }

  download(path: string, query?: Query, fallbackName = 'export.csv') {
    this.http.get(`${API_BASE}${path}`, { params: this.params(query), responseType: 'blob' }).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = fallbackName;
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  patients = {
    search: (search: string, page = 1, pageSize = 20) =>
      this.get<PagedResult<PatientSearchItem>>('/patients', { search, page, pageSize }),
    get: (id: string) => this.get<Patient>(`/patients/${id}`),
    getByNumber: (patientNumber: string) => this.get<Patient>(`/patients/number/${encodeURIComponent(patientNumber)}`),
    create: (body: CreatePatientRequest) => this.post<Patient>('/patients', body),
    renew: (id: string) => this.post<{ patientId: string; periodEnd: string; billId: string; feeAmount: number }>(`/patients/${id}/renew`, {}),
    addEnrollment: (id: string, body: unknown) => this.post<unknown>(`/patients/${id}/hmo-enrollments`, body),
    basicByNumber: (patientNumber: string) =>
      this.get<PatientBasicProfile>(`/patients/number/${encodeURIComponent(patientNumber)}`),
    vitalsByNumber: (patientNumber: string, page = 1, pageSize = 10) =>
      this.get<PatientVitalsPage>(`/patients/number/${encodeURIComponent(patientNumber)}/vitals`, { page, pageSize }),
    encountersByNumber: (patientNumber: string) =>
      this.get<PatientEncounterSummary[]>(`/patients/number/${encodeURIComponent(patientNumber)}/encounters`),
    hmosByNumber: (patientNumber: string) =>
      this.get<PatientHmoEnrollment[]>(`/patients/number/${encodeURIComponent(patientNumber)}/hmo-enrollments`),
    fitnessByNumber: (patientNumber: string) =>
      this.get<FitnessCertificate[]>(`/patients/number/${encodeURIComponent(patientNumber)}/fitness`),
    investigationsByNumber: (patientNumber: string) =>
      this.get<PatientInvestigation[]>(`/patients/number/${encodeURIComponent(patientNumber)}/investigations`),
    surgeriesByNumber: (patientNumber: string) =>
      this.get<PatientSurgery[]>(`/patients/number/${encodeURIComponent(patientNumber)}/surgeries`),
    financeByNumber: (patientNumber: string) =>
      this.get<PatientFinance | null>(`/patients/number/${encodeURIComponent(patientNumber)}/finance`),
    filesByNumber: (patientNumber: string) =>
      this.get<PatientFile[]>(`/patients/number/${encodeURIComponent(patientNumber)}/files`),
    uploadFileByNumber: (patientNumber: string, file: File) => {
      const body = new FormData();
      body.append('file', file);
      return this.http.post<PatientFile>(`${API_BASE}/patients/number/${encodeURIComponent(patientNumber)}/files`, body);
    },
    downloadFileByNumber: (patientNumber: string, fileId: string, fileName: string) =>
      this.download(`/patients/number/${encodeURIComponent(patientNumber)}/files/${fileId}`, undefined, fileName),
    addEnrollmentByNumber: (patientNumber: string, body: unknown) =>
      this.post<unknown>(`/patients/number/${encodeURIComponent(patientNumber)}/hmo-enrollments`, body),
    updateEnrollmentByNumber: (patientNumber: string, enrollmentId: string, body: unknown) =>
      this.put<unknown>(`/patients/number/${encodeURIComponent(patientNumber)}/hmo-enrollments/${enrollmentId}`, body),
  };

  reference = {
    hmos: () => this.get<HmoOption[]>('/reference/hmos'),
    doctors: () => this.get<DoctorOption[]>('/reference/doctors'),
    enrollments: (patientId: string) => this.get<HmoEnrollmentOption[]>(`/reference/patients/${patientId}/hmo-enrollments`),
  };

  clinical = {
    encounter: (id: string) => this.get<Encounter>(`/clinical/encounters/${id}`),
    admission: (id: string) => this.get<unknown>(`/clinical/admissions/${id}`),
    vitals: (body: unknown) => this.post<unknown>('/clinical/vitals', body),
    createOutpatient: (body: unknown) => this.post<Encounter>('/clinical/encounters/outpatient', body),
    createAdmission: (body: unknown) => this.post<unknown>('/clinical/admissions', body),
    addInpatientEncounter: (admissionId: string, body: unknown) => this.post<Encounter>(`/clinical/admissions/${admissionId}/encounters`, body),
    prescription: (body: unknown) => this.post<unknown>('/clinical/prescriptions', body),
    administer: (body: unknown) => this.post<unknown>('/clinical/administrations', body),
    requestDischarge: (admissionId: string, body: unknown) => this.post<unknown>(`/clinical/admissions/${admissionId}/request-discharge`, body),
    completeDischarge: (admissionId: string) => this.post<unknown>(`/clinical/admissions/${admissionId}/complete-discharge`, {}),
  };

  pharmacy = {
    prescription: (encounterId: string) => this.get<PharmacyPrescription>(`/pharmacy/encounters/${encounterId}/prescription`),
    dispense: (body: unknown) => this.post<DispenseResult>('/pharmacy/dispenses', body),
    confirmTariff: (billId: string, lineIds: string[]) => this.post<void>(`/pharmacy/bills/${billId}/confirm-tariff`, { billLineIds: lineIds }),
  };

  billing = {
    search: (status?: string, patientId?: string, page = 1, pageSize = 25) =>
      this.get<PagedResult<Bill>>('/billing/bills', { status, patientId, page, pageSize }),
    get: (id: string) => this.get<Bill>(`/billing/bills/${id}`),
    routeHmo: (id: string) => this.post<Bill>(`/billing/bills/${id}/route-hmo`, {}),
    negotiate: (id: string, discountAmount: number, reason: string) =>
      this.post<Bill>(`/billing/bills/${id}/negotiate`, { discountAmount, reason }),
    pay: (id: string, body: unknown) => this.post<Bill>(`/billing/bills/${id}/payments`, body),
    approve: (id: string, notes: string | null) => this.post<Bill>(`/billing/bills/${id}/approve`, { notes }),
    accountBook: (from: string | null, to: string | null, type: string | null, page = 1) =>
      this.get<PagedResult<AccountBookEntry>>('/billing/account-book', { from, to, type, page, pageSize: 50 }),
    addExpense: (body: unknown) => this.post<AccountBookEntry>('/billing/account-book/expenses', body),
  };

  hmo = {
    authorizations: (patientId?: string, page = 1) => this.get<PagedResult<PriorAuthorization>>('/hmo/prior-authorizations', { patientId, page, pageSize: 25 }),
    saveAuthorization: (body: unknown) => this.post<PriorAuthorization>('/hmo/prior-authorizations', body),
    updateCoverage: (billId: string, body: unknown) => this.post<Bill>(`/hmo/bills/${billId}/coverage`, body),
    approve: (billId: string) => this.post<Bill>(`/hmo/bills/${billId}/approve`, {}),
  };

  fitness = {
    search: (from: string | null, to: string | null, hmoCompanyId: string | null, search: string | null, page = 1) =>
      this.get<PagedResult<FitnessCertificate>>('/fitness', { from, to, hmoCompanyId, search, page, pageSize: 25 }),
    create: (body: unknown) => this.post<FitnessCertificate>('/fitness', body),
    updateClinical: (id: string, investigationResults: Array<{ key: string; value: string }>) =>
      this.post<FitnessCertificate>(`/fitness/${id}/clinical`, { investigationResults }),
    uploadCertificate: (id: string, file: File) => {
      const body = new FormData();
      body.append('file', file);
      return this.http.post<FitnessCertificate>(`${API_BASE}/fitness/${id}/certificate`, body);
    },
    submit: (id: string) => this.post<FitnessCertificate>(`/fitness/${id}/submit`, {}),
    decide: (id: string, approve: boolean, rejectionReason: string | null) =>
      this.post<FitnessCertificate>(`/fitness/${id}/decision`, { approve, rejectionReason }),
    setPayment: (id: string, paid: boolean) => this.post<FitnessCertificate>(`/fitness/${id}/payment`, { paid }),
    batchPayment: (hmoCompanyId: string, certificateIds: string[], paid: boolean) =>
      this.post<FitnessCertificate[]>(`/fitness/hmo/${hmoCompanyId}/batch-payment`, { certificateIds, paid }),
    downloadCertificate: (id: string, fileName: string) => this.download(`/fitness/${id}/certificate`, undefined, fileName),
    markPaid: (id: string) => this.post<FitnessCertificate>(`/fitness/${id}/mark-paid`, {}),
    export: (from: string | null, to: string | null, hmoCompanyId: string | null, search: string | null) =>
      this.download('/fitness/export', { from, to, hmoCompanyId, search }, 'fitness.csv'),
  };

  staff = {
    me: () => this.get<StaffProfile>('/staff/me'),
    get: (id: string) => this.get<StaffProfile>(`/staff/${id}`),
  };

  hr = {
    users: (active?: boolean, page = 1) => this.get<PagedResult<UserRecord>>('/hr/users', { active, page, pageSize: 25 }),
    create: (body: unknown) => this.post<UserRecord>('/hr/users', body),
    payroll: (id: string, body: unknown) => this.put<UserRecord>(`/hr/users/${id}/payroll`, body),
    disable: (id: string, reason: string) => this.post<UserRecord>(`/hr/users/${id}/disable`, { reason }),
    export: (active?: boolean) => this.download('/hr/users/export', { active }, 'users.csv'),
  };

  admin = {
    fees: () => this.get<Fee[]>('/admin/fees'),
    createFee: (body: unknown) => this.post<Fee>('/admin/fees', body),
    hmos: () => this.get<unknown[]>('/admin/hmos'),
    createHmo: (body: unknown) => this.post<unknown>('/admin/hmos', body),
  };

  private params(query?: Query): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
    }
    return params;
  }
}

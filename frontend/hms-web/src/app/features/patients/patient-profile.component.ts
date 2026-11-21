import { Component, OnInit, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import {
  FitnessCertificate, HmoOption, PatientBasicProfile, PatientEncounterSummary, PatientFile,
  PatientFinance, PatientHmoEnrollment, PatientInvestigation, PatientSurgery, PatientVitalsPage,
} from '../../core/models';

type SectionKey = 'hmo' | 'vitals' | 'fitness' | 'encounters' | 'investigations' | 'surgeries' | 'finance' | 'files';

/** Patient chart using patient number in the URL and lazy accordion sections. */
@Component({
  selector: 'app-patient-profile',
  imports: [DatePipe, CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './patient-profile.component.html',
})
export class PatientProfileComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly basic = signal<PatientBasicProfile | null>(null);
  readonly hmos = signal<HmoOption[]>([]);
  readonly hmoAssignments = signal<PatientHmoEnrollment[] | null>(null);
  readonly vitals = signal<PatientVitalsPage | null>(null);
  readonly fitness = signal<FitnessCertificate[] | null>(null);
  readonly encounters = signal<PatientEncounterSummary[] | null>(null);
  readonly investigations = signal<PatientInvestigation[] | null>(null);
  readonly surgeries = signal<PatientSurgery[] | null>(null);
  readonly finance = signal<PatientFinance | null>(null);
  readonly files = signal<PatientFile[] | null>(null);
  readonly vitalsPage = signal(1);
  readonly loading = signal(false);
  readonly sectionLoading = signal<SectionKey | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly editingEnrollment = signal<string | null>(null);
  readonly hmoForm = signal({
    hmoCompanyId: '', hmoMemberId: '', planName: '',
    validFrom: new Date().toISOString().slice(0, 10), validTo: '', isPrimary: false,
  });
  private readonly loaded = new Set<SectionKey>();

  ngOnInit(): void {
    const patientNumber = this.patientNumber;
    if (!patientNumber) { this.toast.error('Patient number is missing.'); return; }
    this.loadBasic();
    if (this.auth.hasPermission('patients.hmo.manage')) {
      this.api.reference.hmos().subscribe({
        next: (hmos) => this.hmos.set(hmos),
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load HMO options.'),
      });
    }
  }

  get patientNumber(): string { return this.route.snapshot.paramMap.get('patientNumber') ?? ''; }

  loadBasic(): void {
    this.loading.set(true);
    this.api.patients.basicByNumber(this.patientNumber)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (profile) => this.basic.set(profile),
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load the patient record.'),
      });
  }

  canSee(section: SectionKey): boolean {
    const basic = this.basic();
    switch (section) {
      case 'hmo': return true;
      case 'vitals': return this.auth.hasPermission('vitals.read');
      case 'fitness': return this.auth.hasPermission('fitness.read');
      case 'encounters':
      case 'investigations':
      case 'surgeries': return this.auth.hasPermission('encounters.read');
      case 'finance': return Boolean(basic?.isPrivate) || this.auth.hasPermission('billing.read');
      case 'files': return true;
      default: return false;
    }
  }

  onToggle(section: SectionKey, event: Event): void {
    const open = (event.target as HTMLDetailsElement).open;
    if (open && !this.loaded.has(section)) this.loadSection(section);
  }

  loadSection(section: SectionKey): void {
    if (!this.canSee(section)) return;
    this.sectionLoading.set(section);
    const fail = (err: unknown) => {
      this.sectionLoading.set(null);
      this.toast.error((err as { error?: { detail?: string } })?.error?.detail ?? 'Could not load this section.');
    };
    switch (section) {
      case 'hmo':
        this.api.patients.hmosByNumber(this.patientNumber).subscribe({ next: (x) => { this.hmoAssignments.set(x); this.finish(section); }, error: fail });
        break;
      case 'vitals':
        this.api.patients.vitalsByNumber(this.patientNumber, 1, 5).subscribe({
          next: (vitals) => { this.vitals.set(vitals); this.vitalsPage.set(1); this.finish(section); }, error: fail,
        });
        break;
      case 'fitness':
        this.api.patients.fitnessByNumber(this.patientNumber).subscribe({ next: (x) => { this.fitness.set(x); this.finish(section); }, error: fail });
        break;
      case 'encounters':
        this.api.patients.encountersByNumber(this.patientNumber).subscribe({ next: (x) => { this.encounters.set(x); this.finish(section); }, error: fail });
        break;
      case 'investigations':
        this.api.patients.investigationsByNumber(this.patientNumber).subscribe({ next: (x) => { this.investigations.set(x); this.finish(section); }, error: fail });
        break;
      case 'surgeries':
        this.api.patients.surgeriesByNumber(this.patientNumber).subscribe({ next: (x) => { this.surgeries.set(x); this.finish(section); }, error: fail });
        break;
      case 'finance':
        this.api.patients.financeByNumber(this.patientNumber).subscribe({ next: (x) => { this.finance.set(x); this.finish(section); }, error: fail });
        break;
      case 'files':
        this.api.patients.filesByNumber(this.patientNumber).subscribe({ next: (x) => { this.files.set(x); this.finish(section); }, error: fail });
        break;
    }
  }

  changeVitalsPage(delta: number): void {
    const vitals = this.vitals();
    if (!vitals) return;
    const totalPages = Math.max(1, Math.ceil(vitals.total / vitals.pageSize));
    const next = Math.min(totalPages, Math.max(1, this.vitalsPage() + delta));
    if (next === this.vitalsPage()) return;
    this.api.patients.vitalsByNumber(this.patientNumber, next, vitals.pageSize).subscribe({
      next: (page) => { this.vitals.set(page); this.vitalsPage.set(next); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load vitals.'),
    });
  }

  editEnrollment(enrollment: PatientHmoEnrollment): void {
    this.editingEnrollment.set(enrollment.id);
    this.hmoForm.set({
      hmoCompanyId: enrollment.hmoCompanyId, hmoMemberId: enrollment.hmoMemberId,
      planName: enrollment.planName ?? '', validFrom: enrollment.validFrom,
      validTo: enrollment.validTo ?? '', isPrimary: enrollment.isPrimary,
    });
  }

  resetHmoForm(): void {
    this.editingEnrollment.set(null);
    this.hmoForm.set({ hmoCompanyId: '', hmoMemberId: '', planName: '', validFrom: new Date().toISOString().slice(0, 10), validTo: '', isPrimary: false });
  }

  saveHmo(): void {
    const form = this.hmoForm();
    if (!form.hmoCompanyId || !form.hmoMemberId) { this.toast.error('HMO and HMO member ID are required.'); return; }
    const body = { ...form, hmoCompanyId: form.hmoCompanyId, hmoMemberId: form.hmoMemberId,
      planName: form.planName || null, validTo: form.validTo || null };
    const editId = this.editingEnrollment();
    const request = editId
      ? this.api.patients.updateEnrollmentByNumber(this.patientNumber, editId, body)
      : this.api.patients.addEnrollmentByNumber(this.patientNumber, body);
    request.subscribe({
      next: () => { this.toast.success(editId ? 'HMO assignment updated.' : 'HMO assignment added.'); this.resetHmoForm(); this.hmoAssignments.set(null); this.loaded.delete('hmo'); this.loadSection('hmo'); this.loadBasic(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not save the HMO assignment.'),
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  uploadFile(): void {
    const file = this.selectedFile();
    if (!file) { this.toast.error('Choose a file to upload.'); return; }
    this.api.patients.uploadFileByNumber(this.patientNumber, file).subscribe({
      next: () => { this.toast.success('File uploaded.'); this.selectedFile.set(null); this.loadSection('files'); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'File upload failed.'),
    });
  }

  download(fileId: string, fileName: string): void {
    this.api.patients.downloadFileByNumber(this.patientNumber, fileId, fileName);
  }

  statusClass(status: string): string {
    if (status === 'Approved' || status === 'Paid' || status === 'Completed' || status === 'Active') return 'badge-green';
    if (status === 'Rejected' || status === 'Cancelled' || status === 'Expired') return 'badge-red';
    if (status === 'PendingDoctorReview' || status === 'Unpaid' || status === 'AwaitingDoctor' || status === 'AwaitingNurse') return 'badge-amber';
    return 'badge-slate';
  }

  private finish(section: SectionKey): void {
    this.loaded.add(section);
    this.sectionLoading.set(null);
  }
}

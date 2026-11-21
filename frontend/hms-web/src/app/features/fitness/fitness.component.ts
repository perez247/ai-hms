import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { FitnessCertificate, FitnessInvestigationResult, HmoOption } from '../../core/models';

const DEFAULT_FINDINGS = (): FitnessInvestigationResult[] => [
  { key: 'Height', value: '' },
  { key: 'Weight', value: '' },
  { key: 'Blood pressure (BP)', value: '' },
  { key: 'Pulse', value: '' },
];

/**
 * Two-stage fitness capture:
 * Reception/Nurse creates basic information (Awaiting Nurse), Nurse adds findings/certificate
 * and sends to Doctor, Doctor decides, Finance settles private records individually.
 */
@Component({
  selector: 'app-fitness',
  imports: [FormsModule, RouterLink],
  templateUrl: './fitness.component.html',
})
export class FitnessComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);
  readonly items = signal<FitnessCertificate[]>([]);
  readonly hmos = signal<HmoOption[]>([]);
  readonly findings = signal<FitnessInvestigationResult[]>(DEFAULT_FINDINGS());
  readonly selectedCertificate = signal<File | null>(null);
  readonly reviewing = signal<FitnessCertificate | null>(null);
  readonly busy = signal(false);

  patientNumber = '';
  patientFullName = '';
  patientAge: number | null = null;
  personSex = 'Female';
  phone = '';
  paymentType = 'Private';
  hmoCompanyId = '';
  fitnessDate = new Date().toISOString().slice(0, 10);
  expiresAt = '';
  findingKey = '';
  findingValue = '';

  ngOnInit(): void {
    this.load();
    if (this.auth.hasPermission('fitness.create')) {
      this.api.reference.hmos().subscribe({
        next: (hmos) => this.hmos.set(hmos),
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load HMO options.'),
      });
    }
  }

  load(): void {
    this.api.fitness.search(null, null, null, null).subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load fitness records.'),
    });
  }

  createBasic(): void {
    if (!this.patientFullName.trim() || !this.patientAge || !this.phone.trim()) {
      this.toast.error('Full name, age and phone are required.');
      return;
    }
    if (this.paymentType === 'Hmo' && !this.hmoCompanyId) {
      this.toast.error('Select the HMO for an HMO fitness record.');
      return;
    }
    this.busy.set(true);
    this.api.fitness.create({
      patientNumber: this.patientNumber.trim() || null,
      patientFullName: this.patientFullName.trim(),
      patientAge: Number(this.patientAge),
      personSex: this.personSex,
      phone: this.phone.trim(),
      paymentType: this.paymentType,
      hmoCompanyId: this.paymentType === 'Hmo' ? this.hmoCompanyId : null,
      hmoEnrollmentId: null,
      fitnessDate: this.fitnessDate,
      expiresAt: this.expiresAt || null,
    }).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Fitness record sent to the Nurse queue.');
        this.resetBasic();
        this.load();
      },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not create the fitness record.'); },
    });
  }

  startNurseReview(item: FitnessCertificate): void {
    this.reviewing.set(item);
    this.findings.set(item.investigationResults.length > 0
      ? item.investigationResults.map((finding) => ({ ...finding }))
      : DEFAULT_FINDINGS());
    this.selectedCertificate.set(null);
  }

  cancelNurseReview(): void {
    this.reviewing.set(null);
    this.selectedCertificate.set(null);
  }

  addFinding(): void {
    if (!this.findingKey.trim() || !this.findingValue.trim()) return;
    this.findings.update((items) => [...items, { key: this.findingKey.trim(), value: this.findingValue.trim() }]);
    this.findingKey = '';
    this.findingValue = '';
  }

  removeFinding(index: number): void {
    this.findings.update((items) => items.filter((_, i) => i !== index));
  }

  setFindingValue(index: number, value: string): void {
    this.findings.update((items) => items.map((item, i) => i === index ? { ...item, value } : item));
  }

  onCertificateSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedCertificate.set(input.files?.[0] ?? null);
  }

  submitToDoctor(): void {
    const record = this.reviewing();
    const file = this.selectedCertificate();
    if (!record) return;
    const validFindings = this.findings().filter((finding) => finding.key.trim() && finding.value.trim());
    if (validFindings.length === 0) { this.toast.error('Enter at least one investigation finding.'); return; }
    if (!file) { this.toast.error('Upload the signed certificate before sending to the Doctor.'); return; }
    this.busy.set(true);
    this.api.fitness.updateClinical(record.id, validFindings).subscribe({
      next: () => {
        this.api.fitness.uploadCertificate(record.id, file).subscribe({
          next: () => {
            this.api.fitness.submit(record.id).subscribe({
              next: () => { this.busy.set(false); this.toast.success('Fitness sent to the Doctor.'); this.cancelNurseReview(); this.load(); },
              error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not send to the Doctor.'); },
            });
          },
          error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not upload the certificate.'); },
        });
      },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not save investigation findings.'); },
    });
  }

  decide(item: FitnessCertificate, approve: boolean): void {
    const reason = approve ? null : prompt('Rejection reason:')?.trim();
    if (!approve && !reason) { this.toast.error('A rejection reason is required.'); return; }
    this.api.fitness.decide(item.id, approve, reason ?? null).subscribe({
      next: () => { this.toast.success(approve ? 'Fitness approved.' : 'Fitness rejected.'); this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not save the Doctor decision.'),
    });
  }

  setPayment(item: FitnessCertificate, paid: boolean): void {
    this.api.fitness.setPayment(item.id, paid).subscribe({
      next: () => { this.toast.success(paid ? 'Marked paid.' : 'Marked unpaid.'); this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not update payment status.'),
    });
  }

  download(item: FitnessCertificate): void {
    if (!item.certificateFileName) { this.toast.error('No certificate is attached.'); return; }
    this.api.fitness.downloadCertificate(item.id, item.certificateFileName);
  }

  exportCsv(): void { this.api.fitness.export(null, null, null, null); }

  statusClass(status: string): string {
    if (status === 'Approved' || status === 'Paid') return 'badge-green';
    if (status === 'Rejected') return 'badge-red';
    if (status === 'AwaitingNurse' || status === 'AwaitingDoctor' || status === 'Unpaid') return 'badge-amber';
    return 'badge-slate';
  }

  private resetBasic(): void {
    this.patientNumber = '';
    this.patientFullName = '';
    this.patientAge = null;
    this.phone = '';
    this.paymentType = 'Private';
    this.hmoCompanyId = '';
    this.fitnessDate = new Date().toISOString().slice(0, 10);
    this.expiresAt = '';
  }
}

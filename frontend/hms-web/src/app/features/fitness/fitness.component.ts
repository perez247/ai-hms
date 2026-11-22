import { Component, OnInit, TemplateRef, ViewChild, ViewContainerRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { RouterLink } from '@angular/router';
import { BrnDialogImports, BrnDialogRef, BrnDialogService } from '@spartan-ng/brain/dialog';
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
 * Reception/Nurse creates basic information (Awaiting Nurse), Nurse adds investigation findings
 * and sends to Doctor, Doctor decides, Finance settles private records individually.
 */
@Component({
  selector: 'app-fitness',
  imports: [FormsModule, RouterLink, BrnDialogImports, DatePipe],
  templateUrl: './fitness.component.html',
})
export class FitnessComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialogs = inject(BrnDialogService);
  private readonly viewContainer = inject(ViewContainerRef);
  private detailRef?: BrnDialogRef<unknown>;

  @ViewChild('detailDialog') private detailDialog?: TemplateRef<unknown>;

  readonly auth = inject(AuthService);
  readonly items = signal<FitnessCertificate[]>([]);
  readonly stageFilter = signal('');
  readonly paymentTypeFilter = signal('');
  readonly paymentStatusFilter = signal('');
  readonly detail = signal<FitnessCertificate | null>(null);
  readonly rejectionReason = signal('');
  readonly hmos = signal<HmoOption[]>([]);
  readonly findings = signal<FitnessInvestigationResult[]>(DEFAULT_FINDINGS());
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
    const q = this.route.snapshot.queryParamMap;
    this.patientNumber = q.get('patientNumber') ?? '';
    this.patientFullName = q.get('patientName') ?? '';
    this.patientAge = q.get('patientAge') ? Number(q.get('patientAge')) : null;
    this.phone = q.get('phone') ?? '';
    this.load();
    if (this.auth.hasPermission('fitness.create')) {
      this.api.reference.hmos().subscribe({
        next: (hmos) => this.hmos.set(hmos),
        error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load HMO options.'),
      });
    }
  }

  /** All three filters combine and are applied on every change. */
  load(): void {
    this.api.fitness.search(null, null, null, null, 1,
      this.stageFilter() || null, this.paymentTypeFilter() || null, this.paymentStatusFilter() || null).subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load fitness records.'),
    });
  }

  setStage(value: string): void { this.stageFilter.set(value); this.load(); }
  setPaymentType(value: string): void { this.paymentTypeFilter.set(value); this.load(); }
  setPaymentStatus(value: string): void { this.paymentStatusFilter.set(value); this.load(); }

  openDetail(item: FitnessCertificate): void {
    this.detail.set(item);
    this.rejectionReason.set('');
    if (!this.detailDialog) return;
    this.detailRef?.close();
    this.detailRef = this.dialogs.open(this.detailDialog, this.viewContainer, undefined,
      { ariaLabel: 'Fitness certificate details' });
  }

  closeDetail(): void {
    this.detailRef?.close();
    this.detailRef = undefined;
    this.detail.set(null);
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
  }

  cancelNurseReview(): void {
    this.reviewing.set(null);
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

  submitToDoctor(): void {
    const record = this.reviewing();
    if (!record) return;
    const validFindings = this.findings().filter((finding) => finding.key.trim() && finding.value.trim());
    if (validFindings.length === 0) { this.toast.error('Enter at least one investigation finding.'); return; }
    this.busy.set(true);
    this.api.fitness.updateClinical(record.id, validFindings).subscribe({
      next: () => {
        this.api.fitness.submit(record.id).subscribe({
          next: () => { this.busy.set(false); this.toast.success('Fitness sent to the Doctor.'); this.cancelNurseReview(); this.load(); },
          error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not send to the Doctor.'); },
        });
      },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not save investigation findings.'); },
    });
  }

  /** Doctor decision is made from the full-detail Spartan dialog, not a browser prompt. */
  decide(item: FitnessCertificate, approve: boolean): void {
    const reason = approve ? null : this.rejectionReason().trim();
    if (!approve && !reason) { this.toast.error('A rejection reason is required.'); return; }
    this.busy.set(true);
    this.api.fitness.decide(item.id, approve, reason).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success(approve ? 'Fitness approved.' : 'Fitness rejected.');
        this.closeDetail();
        this.load();
      },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not save the Doctor decision.'); },
    });
  }

  setPayment(item: FitnessCertificate, paid: boolean): void {
    this.api.fitness.setPayment(item.id, paid).subscribe({
      next: () => { this.toast.success(paid ? 'Marked paid.' : 'Marked unpaid.'); this.load(); },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not update payment status.'),
    });
  }

  exportCsv(): void {
    this.api.fitness.export(null, null, null, null,
      this.stageFilter() || null, this.paymentTypeFilter() || null, this.paymentStatusFilter() || null);
  }

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

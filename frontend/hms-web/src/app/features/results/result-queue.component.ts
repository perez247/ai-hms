import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { ServiceRequestDto } from '../../core/models';

/** Shared result-entry workspace: lab tests and procedures use the same queue and editor. */
@Component({
  selector: 'app-result-queue',
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="mx-auto px-4">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Results workspace</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">{{ kind() === 'Test' ? 'Lab test requests' : 'Procedure requests' }}</h1>
      <p class="mt-1 text-sm text-slate-500">Requests raised by doctors. Results can be entered after encounter sign-off; submitted results are locked.</p>
      <div class="mt-4 flex flex-wrap gap-2">
        <input class="input max-w-sm" placeholder="Search patient, number or item" [(ngModel)]="search" (ngModelChange)="load()" />
        <select class="input w-48" [(ngModel)]="status" (ngModelChange)="load()">
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="InProgress">In progress</option>
          <option value="Completed">Completed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </div>
      <div class="mt-5 grid gap-5 lg:grid-cols-[1fr_420px]">
        <div class="card overflow-hidden">
          <table class="table">
            <thead><tr><th>Request</th><th>Patient</th><th>Item</th><th>Ordered</th><th>Result</th><th></th></tr></thead>
            <tbody>
              @for (row of requests(); track row.id) {
                <tr [class]="selectedId() === row.id ? 'bg-teal-50' : ''">
                  <td class="font-mono text-xs">{{ row.requestNumber }}</td>
                  <td><p class="font-semibold">{{ row.patientName }}</p><p class="text-xs text-slate-500">{{ row.patientNumber }}</p></td>
                  <td>{{ row.itemName }}</td>
                  <td>{{ row.orderedAt | date:'mediumDate' }}</td>
                  <td><span class="badge-slate">{{ row.resultStatus ?? 'Pending' }}</span></td>
                  <td>
                    <button class="btn-secondary" type="button" (click)="select(row)">Open</button>
                    <a class="btn-secondary ml-1" [routerLink]="['/doctor']" [queryParams]="{ encounterId: row.encounterId }">Encounter</a>
                  </td>
                </tr>
              }
              @if (requests().length === 0) { <tr><td colspan="6" class="py-10 text-center text-slate-500">No requests found.</td></tr> }
            </tbody>
          </table>
        </div>
        @if (selected(); as row) {
          <div class="card h-fit p-5">
            <h2 class="text-lg font-bold text-slate-950">{{ row.itemName }}</h2>
            <p class="mt-1 text-xs text-slate-500">{{ row.requestNumber }} · {{ row.patientName }} · v{{ row.version }}</p>
            <p class="mt-2 text-xs text-slate-500">Funding: {{ row.fundingSource }} · ordered by {{ row.doctorName }}</p>
            <div class="mt-4 space-y-3">
              <div><label class="label">Result / findings</label><textarea class="input" rows="6" [disabled]="selectedLocked" [(ngModel)]="resultText"></textarea></div>
              <div><label class="label">Structured notes (optional)</label><textarea class="input" rows="2" [disabled]="selectedLocked" [(ngModel)]="findings"></textarea></div>
              <label class="flex items-center gap-2 text-sm"><input type="checkbox" [disabled]="selectedLocked" [(ngModel)]="abnormalFlag" /> Abnormal finding</label>
            </div>
            @if (selectedLocked) {
              <p class="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">This result has been submitted and is locked.</p>
            } @else {
              <div class="mt-4 flex gap-2">
                <button class="btn-secondary" type="button" (click)="save()">Save draft</button>
                <button class="btn-primary" type="button" (click)="submit()">Submit result</button>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class ResultQueueComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly kind = signal<'Test' | 'Procedure'>('Test');
  readonly requests = signal<ServiceRequestDto[]>([]);
  readonly selected = signal<ServiceRequestDto | null>(null);
  readonly selectedId = signal<string | null>(null);
  search = '';
  status = '';
  resultText = '';
  findings = '';
  abnormalFlag = false;

  get selectedLocked(): boolean {
    const status = this.selected()?.resultStatus;
    return status === 'Submitted' || status === 'Amended';
  }

  ngOnInit(): void {
    this.kind.set(this.route.snapshot.data['kind'] === 'Procedure' ? 'Procedure' : 'Test');
    this.load();
  }

  load(): void {
    const call = this.kind() === 'Test' ? this.api.results.tests : this.api.results.procedures;
    call(this.status || null, this.search || null).subscribe({
      next: (rows) => this.requests.set(rows),
      error: (err) => void err,
    });
  }

  select(row: ServiceRequestDto): void {
    this.selected.set(row);
    this.selectedId.set(row.id);
    this.resultText = row.resultText ?? '';
    this.findings = row.findings ?? '';
    this.abnormalFlag = row.abnormalFlag ?? false;
  }

  private body() {
    return {
      resultText: this.resultText.trim() || null,
      findings: this.findings.trim() || null,
      structuredJson: null,
      abnormalFlag: this.abnormalFlag,
      reason: 'Result entry',
    };
  }

  save(): void {
    const row = this.selected();
    if (!row || this.selectedLocked) return;
    this.api.results.save(row.id, this.body()).subscribe({
      next: (updated) => { this.apply(updated); this.toast.success('Result saved.'); },
      error: (err) => void err,
    });
  }

  submit(): void {
    const row = this.selected();
    if (!row || this.selectedLocked) return;
    if (!this.resultText.trim()) { this.toast.error('Enter a result before submitting.'); return; }
    this.api.results.submit(row.id, { ...this.body(), resultText: this.resultText.trim() }).subscribe({
      next: (updated) => { this.apply(updated); this.toast.success('Result submitted.'); },
      error: (err) => void err,
    });
  }

  private apply(updated: ServiceRequestDto): void {
    this.requests.update((rows) => rows.map((row) => row.id === updated.id ? updated : row));
    this.select(updated);
    this.load();
  }
}

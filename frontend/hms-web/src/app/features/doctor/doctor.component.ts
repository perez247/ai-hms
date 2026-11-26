import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AuthService } from '../../core/auth.service';
import { DoctorQueue, DoctorQueuePatient, Encounter, EncounterListItem } from '../../core/models';

interface DoctorRow extends EncounterListItem {
  /** Pending rows come from the queue and have no encounter yet. */
  pending: boolean;
  referenceNumber: string;
  hmoName: string | null;
  sourceId: string;
  openEncounterId: string | null;
}

type SortColumn = 'patientName' | 'type' | 'status' | 'startedAt';

/** Doctor worklist: pending visits and existing encounters. Opening a row navigates to its own page. */
@Component({
  selector: 'app-doctor',
  imports: [DatePipe],
  templateUrl: './doctor.component.html',
})
export class DoctorComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly queue = signal<DoctorQueue | null>(null);
  readonly encounters = signal<EncounterListItem[]>([]);
  readonly notice = signal<string | null>(null);

  // Worklist filters and sorting. Pending + open is the default view.
  readonly filterType = signal('');
  readonly filterStatus = signal('active');
  readonly filterDate = signal('');
  readonly search = signal('');
  readonly sortColumn = signal<SortColumn>('startedAt');
  readonly sortAscending = signal(false);

  /** Pending appointments/admissions and existing encounters in one sortable, filterable table. */
  readonly rows = computed<DoctorRow[]>(() => {
    const queue = this.queue();
    const pending: DoctorRow[] = [];
    if (queue) {
      for (const item of queue.awaitingOutpatients) {
        // A pending visit with an open encounter is already represented by the encounter row.
        if (!item.hasOpenEncounter) pending.push(this.toPendingRow(item, 'Outpatient'));
      }
      for (const item of queue.activeInpatients) {
        if (!item.hasOpenEncounter) pending.push(this.toPendingRow(item, 'Inpatient'));
      }
    }
    return [
      ...pending,
      ...this.encounters().map((encounter) => ({
        ...encounter,
        pending: false,
        referenceNumber: encounter.appointmentNumber ?? encounter.admissionNumber ?? encounter.encounterNumber,
        hmoName: null,
        sourceId: encounter.id,
        openEncounterId: null,
      })),
    ];
  });

  readonly filteredRows = computed(() => {
    const type = this.filterType();
    const status = this.filterStatus();
    const date = this.filterDate();
    const searchTerm = this.search().trim().toLowerCase();
    const rows = this.rows().filter((row) => {
      if (type && row.type !== type) return false;
      if (status === 'active' && row.status !== 'Pending' && row.status !== 'Open') return false;
      if (status === 'closed' && row.status !== 'Completed' && row.status !== 'Cancelled') return false;
      if (status && status !== 'active' && status !== 'closed' && row.status !== status) return false;
      if (date && !row.startedAt.startsWith(date)) return false;
      if (searchTerm) {
        const haystack = [row.encounterNumber, row.patientNumber, row.patientName, row.referenceNumber,
          row.id, row.presentingComplaint ?? ''].join(' ').toLowerCase();
        if (!haystack.includes(searchTerm)) return false;
      }
      return true;
    });
    const column = this.sortColumn();
    const direction = this.sortAscending() ? 1 : -1;
    return [...rows].sort((a, b) => {
      const left = String(a[column] ?? '').toLowerCase();
      const right = String(b[column] ?? '').toLowerCase();
      return left.localeCompare(right) * direction;
    });
  });

  ngOnInit(): void {
    this.reload();
    // Keep old deep links working by redirecting ?encounterId=... to the encounter page.
    const encounterId = this.route.snapshot.queryParamMap.get('encounterId');
    if (encounterId) void this.router.navigate(['/doctor/encounters', encounterId]);
  }

  private toPendingRow(item: DoctorQueuePatient, type: string): DoctorRow {
    return {
      id: item.sourceId,
      encounterNumber: item.referenceNumber,
      patientId: item.patientId,
      patientNumber: item.patientNumber,
      patientName: item.patientName,
      doctorName: this.auth.user()?.fullName ?? 'Unassigned',
      type,
      status: 'Pending',
      isEmergency: false,
      startedAt: item.since,
      appointmentId: type === 'Outpatient' ? item.sourceId : null,
      appointmentNumber: type === 'Outpatient' ? item.referenceNumber : null,
      admissionId: type === 'Inpatient' ? item.sourceId : null,
      admissionNumber: type === 'Inpatient' ? item.referenceNumber : null,
      presentingComplaint: null,
      pending: true,
      referenceNumber: item.referenceNumber,
      hmoName: item.hmoName,
      sourceId: item.sourceId,
      openEncounterId: item.openEncounterId,
    };
  }

  reload(): void {
    this.loadQueue();
    this.api.clinical.encounters().subscribe({
      next: (rows) => this.encounters.set(rows),
      error: (err) => void err,
    });
  }

  loadQueue(): void {
    this.api.clinical.doctorQueue().subscribe({
      next: (queue) => this.queue.set(queue),
      error: (err) => void err,
    });
  }

  sortBy(column: SortColumn): void {
    if (this.sortColumn() === column) this.sortAscending.update((value) => !value);
    else { this.sortColumn.set(column); this.sortAscending.set(true); }
  }

  clearFilters(): void {
    this.filterType.set(''); this.filterStatus.set('active'); this.filterDate.set(''); this.search.set('');
  }

  /** Pending rows create an encounter; both paths end on the encounter's own page. */
  openRow(row: DoctorRow): void {
    if (row.pending) {
      const item = this.queue()?.awaitingOutpatients.find((x) => x.sourceId === row.sourceId)
        ?? this.queue()?.activeInpatients.find((x) => x.sourceId === row.sourceId);
      if (item) {
        if (row.type === 'Outpatient') this.startOutpatient(item);
        else this.startInpatient(item);
      }
      return;
    }
    void this.router.navigate(['/doctor/encounters', row.id]);
  }

  startOutpatient(item: DoctorQueuePatient): void {
    const doctorId = this.auth.user()?.id;
    if (!doctorId) return;
    this.api.clinical.createOutpatient({
      patientId: item.patientId, doctorId, appointmentId: item.sourceId,
      isEmergency: false, hmoEnrollmentId: null,
      presentingComplaint: `Checked-in appointment ${item.referenceNumber}`,
    }).subscribe({
      next: (encounter) => this.afterEncounterCreated(encounter, item.patientName),
      error: (err) => void err,
    });
  }

  startInpatient(item: DoctorQueuePatient): void {
    const doctorId = this.auth.user()?.id;
    if (!doctorId) return;
    this.api.clinical.addInpatientEncounter(item.sourceId, {
      doctorId, presentingComplaint: `Inpatient review ${item.referenceNumber}`,
    }).subscribe({
      next: (encounter) => this.afterEncounterCreated(encounter, item.patientName),
      error: (err) => void err,
    });
  }

  private afterEncounterCreated(encounter: Encounter, patientName: string): void {
    this.notice.set(`Encounter ${encounter.encounterNumber} created for ${patientName}.`);
    this.reload();
    void this.router.navigate(['/doctor/encounters', encounter.id]);
  }
}

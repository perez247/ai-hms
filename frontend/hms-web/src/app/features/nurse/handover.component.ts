import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { ShiftHandover } from '../../core/models';
import { RichTextEditorComponent } from '../shared/rich-text-editor.component';

/** Nurse-only service page for chronological shift handovers. */
@Component({
  selector: 'app-handover',
  imports: [DatePipe, RichTextEditorComponent],
  template: `
    <div class="mx-auto max-w-6xl">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Services</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Shift handover</h1>
      <p class="mt-1 text-sm text-slate-500">Nurse-only chronological log. Rich text is stored with formatting intact.</p>
      <div class="mt-5 grid gap-5 lg:grid-cols-[420px_1fr]">
        <div class="card">
          <div class="card-header"><h2 class="font-bold">New handover</h2></div>
          <div class="card-body space-y-3">
            <app-rich-text-editor [value]="content()" (valueChange)="content.set($event)" />
            <button class="btn-primary w-full" type="button" [disabled]="busy()" (click)="save()">{{ busy() ? 'Posting…' : 'Post handover' }}</button>
          </div>
        </div>
        <div class="card">
          <div class="card-header flex items-center justify-between"><h2 class="font-bold">History</h2><button class="btn-secondary" type="button" (click)="load()">Refresh</button></div>
          <div class="card-body space-y-4">
            @for (entry of entries(); track entry.id) {
              <article class="rounded-xl border border-slate-200 p-4">
                <div class="mb-2 flex items-center justify-between gap-2"><p class="text-sm font-bold">{{ entry.nurseName }} · {{ entry.shiftDate | date:'mediumDate' }}</p><p class="text-xs text-slate-400">{{ entry.createdAt | date:'medium' }}</p></div>
                <div class="prose prose-sm max-w-none text-sm text-slate-700" [innerHTML]="entry.contentHtml"></div>
              </article>
            }
            @if (entries().length === 0) { <p class="text-sm text-slate-500">No handover entries yet.</p> }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class HandoverComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  readonly entries = signal<ShiftHandover[]>([]);
  readonly content = signal('');
  readonly busy = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.nurse.handovers(1, 100).subscribe({
      next: (rows) => this.entries.set(rows),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load handovers.'),
    });
  }

  save(): void {
    if (!this.content().trim()) { this.toast.error('Write a handover before posting.'); return; }
    this.busy.set(true);
    this.api.nurse.createHandover({ shiftDate: new Date().toISOString().slice(0, 10), contentHtml: this.content() }).subscribe({
      next: () => { this.busy.set(false); this.toast.success('Shift handover posted.'); this.content.set(''); this.load(); },
      error: (err) => { this.busy.set(false); this.toast.error(err?.error?.detail ?? 'Could not post the handover.'); },
    });
  }
}

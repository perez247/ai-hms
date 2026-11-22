import { Component, OnInit, TemplateRef, ViewChild, ViewContainerRef, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { BrnButtonImports } from '@spartan-ng/brain/button';
import { BrnDialogImports, BrnDialogRef, BrnDialogService } from '@spartan-ng/brain/dialog';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { DispenseResult, PharmacyPrescription, PharmacyQueueItem, PrescriptionPriceOptions } from '../../core/models';

/**
 * Pharmacy commit-to-Finance screen. Quantities default to min(remaining, stock);
 * submitting calls the dispense command and surfaces the resulting bill status.
 */
@Component({
  selector: 'app-pharmacy',
  imports: [CurrencyPipe, DatePipe, BrnButtonImports, BrnDialogImports],
  templateUrl: './pharmacy.component.html',
})
export class PharmacyComponent implements OnInit {
  protected readonly Math = Math;
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialogs = inject(BrnDialogService);
  private readonly viewContainer = inject(ViewContainerRef);
  private confirmRef?: BrnDialogRef<unknown>;

  @ViewChild('confirmDialog') private confirmDialog?: TemplateRef<unknown>;

  readonly queue = signal<PharmacyQueueItem[]>([]);
  readonly prescription = signal<PharmacyPrescription | null>(null);
  readonly quantities = signal<Record<string, number>>({});
  readonly result = signal<DispenseResult | null>(null);
  readonly loading = signal(false);
  readonly dispensing = signal(false);
  readonly confirmOpen = signal(false);
  readonly encounterId = signal('');
  readonly priceOptions = signal<PrescriptionPriceOptions | null>(null);
  readonly selectedPayer = signal<string>('Private');
  readonly selectedOption = computed(() =>
    this.priceOptions()?.options.find((option) => option.payerType === this.selectedPayer()) ?? null);

  // Only positive quantities are sent; estimates use HMO tariff prices where available.
  readonly selectedItems = computed(() => {
    const prescription = this.prescription();
    if (!prescription) return [];
    return prescription.items
      .map((item) => ({ ...item, quantity: Number(this.quantities()[item.id] ?? 0) }))
      .filter((item) => item.quantity > 0);
  });
  readonly total = computed(() => this.selectedItems().reduce((sum, item) => sum + item.quantity * (item.hmoUnitPrice ?? item.unitPrice), 0));
  readonly patientEstimate = computed(() => this.selectedItems().reduce((sum, item) => {
    const price = item.hmoUnitPrice ?? item.unitPrice;
    const net = item.quantity * price;
    const covered = item.hmoCoveragePercent ?? 0;
    return sum + (net - net * covered / 100);
  }, 0));
  readonly hmoEstimate = computed(() => this.total() - this.patientEstimate());

  ngOnInit(): void {
    this.loadQueue();
    const encounterId = this.route.snapshot.queryParamMap.get('encounterId');
    if (encounterId) {
      this.encounterId.set(encounterId);
      this.load(encounterId);
    }
  }

  loadQueue(): void {
    this.api.get<PharmacyQueueItem[]>('/pharmacy/queue').subscribe({
      next: (queue) => this.queue.set(queue),
      error: (err) => this.toast.error(err?.error?.detail ?? 'Could not load the pharmacy queue.'),
    });
  }

  load(encounterId = this.encounterId(), clearResult = true): void {
    if (!encounterId) return;
    this.loading.set(true);
    if (clearResult) this.result.set(null);
    this.encounterId.set(encounterId);
    this.api.pharmacy.prescription(encounterId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (prescription) => {
          this.prescription.set(prescription);
          const defaults: Record<string, number> = {};
          for (const item of prescription.items) {
            defaults[item.id] = Math.max(0, Math.min(item.quantityPrescribed - item.quantityDispensed, item.stockOnHand));
          }
          this.quantities.set(defaults);
          this.loadPriceOptions(encounterId);
        },
        error: (err) => { this.prescription.set(null); this.toast.error(err?.error?.detail ?? 'Could not load the prescription.'); },
      });
  }

  /** Read-only comparison of private and patient-HMO prices; it never changes the encounter payer. */
  loadPriceOptions(encounterId: string): void {
    this.api.pharmacy.priceOptions(encounterId).subscribe({
      next: (options) => {
        this.priceOptions.set(options);
        const current = options.options[0];
        if (current && !options.options.some((option) => option.payerType === this.selectedPayer())) {
          this.selectedPayer.set(current.payerType);
        }
      },
      error: () => this.priceOptions.set(null),
    });
  }

  selectPayer(payerType: string): void { this.selectedPayer.set(payerType); }

  setQuantity(itemId: string, value: string, max: number): void {
    const quantity = Math.max(0, Math.min(max, Number(value) || 0));
    this.quantities.update((current) => ({ ...current, [itemId]: quantity }));
  }

  canDispense(): boolean {
    return this.selectedItems().length > 0 && !this.dispensing() && this.prescription()?.status !== 'Dispensed';
  }

  openConfirm(): void {
    if (!this.canDispense() || !this.confirmDialog) return;
    this.confirmOpen.set(true);
    this.confirmRef?.close();
    this.confirmRef = this.dialogs.open(this.confirmDialog, this.viewContainer, undefined,
      { ariaLabel: 'Confirm dispense' });
  }

  closeConfirm(): void {
    this.confirmRef?.close();
    this.confirmRef = undefined;
    this.confirmOpen.set(false);
  }

  /** Sends only selected lines and reloads the queue while retaining the receipt banner. */
  dispense(): void {
    const prescription = this.prescription();
    if (!prescription || !this.canDispense()) return;
    this.dispensing.set(true);
    const body = {
      prescriptionId: prescription.id,
      items: this.selectedItems().map((item) => ({ prescriptionItemId: item.id, quantity: item.quantity })),
    };
    this.api.pharmacy.dispense(body)
      .pipe(finalize(() => this.dispensing.set(false)))
      .subscribe({
        next: (result) => {
          this.closeConfirm();
          this.loadQueue();
          this.load(prescription.encounterId, false);
          this.result.set(result);
        },
        error: (err) => {
          this.closeConfirm();
          this.toast.error(err?.error?.detail ?? 'Dispense failed.');
        },
      });
  }

  remaining(item: { quantityPrescribed: number; quantityDispensed: number }): number {
    return item.quantityPrescribed - item.quantityDispensed;
  }
}

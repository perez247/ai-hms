import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ToastService } from '../../core/toast.service';
import { AuthService } from '../../core/auth.service';
import { ImportResult, PayerOption, StockItem } from '../../core/models';

/** Pharmacy/Admin-only catalogue: items hold quantity; prices are attached per payer independently. */
@Component({
  selector: 'app-stocks',
  imports: [FormsModule, DecimalPipe],
  template: `
    <div class="mx-auto px-4">
      <p class="text-xs font-bold uppercase tracking-[0.2em] text-teal-700">Catalogue</p>
      <h1 class="mt-2 text-3xl font-black tracking-tight text-slate-950">Stock items &amp; prices</h1>
      <p class="mt-1 text-sm text-slate-500">Create products first; attach private and HMO prices afterwards.</p>

      <div class="mt-4 flex flex-wrap gap-2">
        <select class="input w-48" [(ngModel)]="type" (ngModelChange)="load()">
          <option value="">All item types</option><option>Medication</option><option>Test</option><option>Procedure</option><option>Service</option>
        </select>
        <select class="input w-56" [(ngModel)]="payerId" (ngModelChange)="load()">
          <option value="">All payers</option>
          @for (payer of payers(); track payer.id) { <option [value]="payer.id">{{ payer.name }}</option> }
        </select>
        <input class="input max-w-sm" placeholder="Search code or name" [(ngModel)]="search" (ngModelChange)="load()" />
      </div>

      @if (auth.hasPermission('stock.manage')) {
        <div class="mt-5 grid gap-5 lg:grid-cols-2">
          <form class="card p-5" (ngSubmit)="create()">
            <h2 class="font-bold text-slate-900">Add / edit stock item</h2>
            <div class="mt-3 grid gap-3 md:grid-cols-2">
              <input class="input" placeholder="Code" [(ngModel)]="code" name="code" />
              <input class="input" placeholder="Name" [(ngModel)]="name" name="name" />
              <select class="input" [(ngModel)]="newType" name="type"><option>Medication</option><option>Test</option><option>Procedure</option><option>Service</option></select>
              <input class="input" type="number" min="0" placeholder="Quantity" [(ngModel)]="quantity" name="quantity" />
            </div>
            <div class="mt-3 flex gap-2">
              <button class="btn-primary" type="submit">{{ editingId() ? 'Save changes' : 'Add item' }}</button>
              @if (editingId()) { <button class="btn-secondary" type="button" (click)="resetItemForm()">Cancel</button> }
            </div>
          </form>

          <form class="card p-5" (ngSubmit)="setPrice()">
            <h2 class="font-bold text-slate-900">Add / update price</h2>
            <div class="mt-3 grid gap-3 md:grid-cols-2">
              <select class="input" [(ngModel)]="priceItemId" name="priceItem">
                <option value="">Select item</option>
                @for (item of allItems(); track item.id) { <option [value]="item.id">{{ item.code }} — {{ item.name }}</option> }
              </select>
              <select class="input" [(ngModel)]="pricePayerId" name="pricePayer">
                <option value="">Select payer</option>
                @for (payer of payers(); track payer.id) { <option [value]="payer.id">{{ payer.name }}</option> }
              </select>
              <input class="input" type="number" min="0" step="0.01" placeholder="Price" [(ngModel)]="priceValue" name="price" />
            </div>
            <button class="btn-primary mt-3" type="submit" [disabled]="!priceItemId || !pricePayerId">Save price</button>
          </form>
        </div>
      }

      <div class="card mt-5 overflow-hidden">
        <table class="table">
          <thead><tr><th>Item</th><th>Type</th><th>Quantity</th><th>Prices</th><th></th></tr></thead>
          <tbody>
            @for (item of items(); track item.id) {
              <tr>
                <td><p class="font-semibold">{{ item.name }} @if (!item.isActive) { <span class="badge-red">Disabled</span> }</p><p class="font-mono text-xs text-slate-500">{{ item.code }}</p></td>
                <td><span class="badge-blue">{{ item.itemType }}</span></td>
                <td>{{ item.quantity | number:'1.0-3' }}</td>
                <td>
                  @for (price of item.prices; track price.id) {
                    <p class="text-xs">{{ price.payerName }}: ₦{{ price.unitPrice | number:'1.2-2' }}</p>
                  }
                  @if (item.prices.length === 0) { <span class="text-xs text-rose-600">No prices attached</span> }
                </td>
                <td>
                  @if (auth.hasPermission('stock.manage')) {
                    <button class="btn-secondary" type="button" (click)="edit(item)">Edit</button>
                    <button class="btn-secondary ml-1" type="button" (click)="toggleActive(item)">{{ item.isActive ? 'Disable' : 'Enable' }}</button>
                  }
                  @if (auth.hasPermission('pharmacy.stock.manage')) {
                    <button class="btn-secondary ml-1" type="button" (click)="receive(item)">+ Stock</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      @if (auth.hasPermission('stock.manage')) {
        <div class="card mt-5 p-5">
          <h2 class="font-bold text-slate-900">Bulk upload</h2>
          <div class="mt-2 flex flex-wrap gap-3 text-sm">
            <a class="text-teal-700 underline" [href]="api.stocks.templateUrl('items', 'csv')">Item template (CSV)</a>
            <a class="text-teal-700 underline" [href]="api.stocks.templateUrl('items', 'xlsx')">Item template (Excel)</a>
            <a class="text-teal-700 underline" [href]="api.stocks.templateUrl('prices', 'csv')">Price template (CSV)</a>
            <a class="text-teal-700 underline" [href]="api.stocks.templateUrl('prices', 'xlsx')">Price template (Excel)</a>
          </div>
          <div class="mt-3 flex flex-wrap gap-2">
            <input type="file" accept=".csv,.xlsx" (change)="onFile($event)" />
            <button class="btn-secondary" type="button" (click)="upload('items')" [disabled]="!file">Import items</button>
            <button class="btn-secondary" type="button" (click)="upload('prices')" [disabled]="!file">Import prices</button>
          </div>
          @if (importResult(); as result) {
            <div class="mt-3 text-sm">
              <p>Imported {{ result.imported }} of {{ result.totalRows }} rows.</p>
              @if (result.errors.length > 0) {
                <table class="table mt-2">
                  <thead><tr><th>Row</th><th>Column</th><th>Error</th></tr></thead>
                  <tbody>@for (error of result.errors; track error.row + error.column) {
                    <tr><td>{{ error.row }}</td><td>{{ error.column }}</td><td class="text-rose-700">{{ error.message }}</td></tr>
                  }</tbody>
                </table>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class StocksComponent implements OnInit {
  readonly api = inject(ApiService);
  readonly toast = inject(ToastService);
  readonly auth = inject(AuthService);

  readonly items = signal<StockItem[]>([]);
  readonly allItems = signal<StockItem[]>([]);
  readonly payers = signal<PayerOption[]>([]);
  readonly importResult = signal<ImportResult | null>(null);
  readonly editingId = signal<string | null>(null);

  type = '';
  payerId = '';
  search = '';
  code = '';
  name = '';
  newType: 'Medication' | 'Test' | 'Procedure' | 'Service' = 'Medication';
  quantity = 0;
  priceItemId = '';
  pricePayerId = '';
  priceValue = 0;
  file: File | null = null;

  ngOnInit(): void {
    this.api.stocks.payers().subscribe({ next: (rows) => this.payers.set(rows) });
    this.load();
  }

  load(): void {
    this.api.stocks.list(this.type || null, this.search || null, this.payerId || null).subscribe({
      next: (rows) => this.items.set(rows),
      error: (err) => void err,
    });
    this.api.stocks.list().subscribe({ next: (rows) => this.allItems.set(rows) });
  }

  edit(item: StockItem): void {
    this.editingId.set(item.id);
    this.code = item.code;
    this.name = item.name;
    this.newType = item.itemType;
    this.quantity = item.quantity;
  }

  resetItemForm(): void {
    this.editingId.set(null);
    this.code = ''; this.name = ''; this.quantity = 0;
  }

  create(): void {
    const body = { code: this.code, name: this.name, itemType: this.newType, quantity: Number(this.quantity),
      unit: 'unit', tariffCode: null, category: null, isActive: true };
    const call = this.editingId()
      ? this.api.stocks.update(this.editingId()!, body)
      : this.api.stocks.create(body);
    call.subscribe({
      next: () => { this.toast.success(this.editingId() ? 'Stock item updated.' : 'Stock item created.'); this.resetItemForm(); this.load(); },
      error: (err) => void err,
    });
  }

  setPrice(): void {
    this.api.stocks.setPrice(this.priceItemId, {
      payerId: this.pricePayerId, unitPrice: Number(this.priceValue), coveragePercent: 100,
    }).subscribe({
      next: () => { this.toast.success('Price saved.'); this.priceValue = 0; this.load(); },
      error: (err) => void err,
    });
  }

  toggleActive(item: StockItem): void {
    this.api.stocks.update(item.id, {
      code: item.code, name: item.name, itemType: item.itemType, quantity: item.quantity,
      isActive: !item.isActive, category: item.category,
    }).subscribe({ next: () => { this.toast.success(item.isActive ? 'Medication disabled.' : 'Medication enabled.'); this.load(); } });
  }

  receive(item: StockItem): void {
    const amount = Number(window.prompt(`Receive quantity for ${item.name}`, '10') ?? 0);
    if (!amount || amount <= 0) return;
    this.api.stocks.adjust(item.id, { quantityDelta: amount, notes: 'Stock receipt' }).subscribe({
      next: () => { this.toast.success('Stock received.'); this.load(); },
      error: (err) => void err,
    });
  }

  onFile(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  upload(kind: 'items' | 'prices'): void {
    if (!this.file) return;
    const call = kind === 'items' ? this.api.stocks.importItems(this.file) : this.api.stocks.importPrices(this.file);
    call.subscribe({
      next: (result) => { this.importResult.set(result); this.load(); this.toast.success(`Imported ${result.imported} rows.`); },
      error: (err) => void err,
    });
  }
}

import { Component, input, output } from '@angular/core';
import {
  BrnSelect, BrnSelectContent, BrnSelectItem, BrnSelectList, BrnSelectTrigger,
} from '@spartan-ng/brain/select';
import { BrnPopover, BrnPopoverContent, BrnPopoverTrigger } from '@spartan-ng/brain/popover';
import { CatalogOption } from './clerking.models';

/**
 * Searchable stock picker built on Spartan UI's select primitives.
 * Typing in the panel calls the same debounced API search the old datalist used; choosing an option
 * emits the selected catalogue item so the caller can set both its id and display label.
 */
@Component({
  selector: 'app-stock-select',
  imports: [BrnSelect, BrnSelectTrigger, BrnSelectContent, BrnSelectList, BrnSelectItem,
    BrnPopover, BrnPopoverTrigger, BrnPopoverContent],
  template: `
    <div brnSelect [value]="selectedId()" (valueChange)="onValueChange($event)" [disabled]="disabled()">
      <div brnPopover class="relative block" sideOffset="4" align="start">
        <button brnPopoverTrigger brnSelectTrigger type="button"
          class="input flex w-full items-center justify-between gap-2 text-left">
          <span class="truncate">{{ display() || placeholder() }}</span>
          <span aria-hidden="true" class="text-slate-400">▾</span>
        </button>
        <ng-template brnPopoverContent [class]="'z-50 w-[var(--brn-select-width)] rounded-xl border border-slate-200 bg-white shadow-lg'">
          <div brnSelectContent class="max-h-80 overflow-y-auto">
            <div class="sticky top-0 border-b border-slate-100 bg-white p-2">
              <input class="input" type="text" [value]="query()" (input)="onQueryInput($event)"
                (keydown)="$event.stopPropagation()" [placeholder]="searchPlaceholder()" />
              @if (loading()) { <p class="mt-1 text-xs text-slate-400">Searching…</p> }
              @else if (searched() && options().length === 0) { <p class="mt-1 text-xs text-slate-400">No matches found.</p> }
            </div>
            <div brnSelectList class="p-1">
              @for (option of options(); track option.id) {
                <button type="button" brnSelectItem [value]="option.id"
                  class="flex w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-sm hover:bg-teal-50"
                  [class.bg-teal-50]="option.id === selectedId()">
                  <span><span class="font-mono text-xs text-slate-500">{{ option.code }}</span> · {{ option.name }}</span>
                  @if (option.detail) { <span class="shrink-0 text-xs text-slate-500">{{ option.detail }}</span> }
                </button>
              }
            </div>
          </div>
        </ng-template>
      </div>
    </div>
  `,
})
export class StockSelectComponent {
  readonly options = input<CatalogOption[]>([]);
  readonly selectedId = input<string>('');
  readonly display = input<string>('');
  readonly placeholder = input('Type at least 2 characters to search…');
  readonly searchPlaceholder = input('Search name or code…');
  readonly loading = input(false);
  readonly searched = input(false);
  readonly disabled = input(false);

  readonly query = input<string>('');
  readonly queryChange = output<string>();
  readonly selected = output<CatalogOption>();

  onQueryInput(event: Event): void {
    this.queryChange.emit((event.target as HTMLInputElement).value);
  }

  onValueChange(id: string | null | undefined): void {
    const option = this.options().find((item) => item.id === id);
    if (option) this.selected.emit(option);
  }
}

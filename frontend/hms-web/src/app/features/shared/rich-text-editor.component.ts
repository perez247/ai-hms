import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';

/** Lightweight WYSIWYG editor backed by contenteditable; stores emitted HTML with formatting intact. */
@Component({
  selector: 'app-rich-text-editor',
  template: `
    <div class="rounded-xl border border-slate-300 bg-white">
      <div class="flex flex-wrap gap-1 border-b border-slate-200 p-2">
        <button type="button" class="rounded px-2 py-1 text-xs font-bold hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('bold')">B</button>
        <button type="button" class="rounded px-2 py-1 text-xs italic hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('italic')">I</button>
        <button type="button" class="rounded px-2 py-1 text-xs underline hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('underline')">U</button>
        <button type="button" class="rounded px-2 py-1 text-xs hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('insertUnorderedList')">• List</button>
        <button type="button" class="rounded px-2 py-1 text-xs hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('insertOrderedList')">1. List</button>
        <button type="button" class="rounded px-2 py-1 text-xs hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('formatBlock', 'h3')">Heading</button>
        <button type="button" class="rounded px-2 py-1 text-xs hover:bg-slate-100" (mousedown)="$event.preventDefault()" (click)="format('removeFormat')">Clear</button>
      </div>
      <div #editor class="min-h-40 p-3 text-sm leading-6 outline-none" contenteditable="true" (input)="emit()" data-placeholder="Write the shift handover…"></div>
    </div>
  `,
})
export class RichTextEditorComponent implements AfterViewInit, OnChanges {
  @ViewChild('editor') editor?: ElementRef<HTMLDivElement>;
  @Input() value = '';
  @Output() valueChange = new EventEmitter<string>();

  ngAfterViewInit(): void { if (this.editor) this.editor.nativeElement.innerHTML = this.value || ''; }
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['value'] && this.editor && this.editor.nativeElement.innerHTML !== this.value) {
      this.editor.nativeElement.innerHTML = this.value || '';
    }
  }

  format(command: string, value?: string): void {
    this.editor?.nativeElement.focus();
    document.execCommand(command, false, value);
    this.emit();
  }

  emit(): void { this.valueChange.emit(this.editor?.nativeElement.innerHTML ?? ''); }
}

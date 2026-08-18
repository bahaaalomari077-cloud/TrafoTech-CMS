import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { FormBlock, FormFieldItem } from '../../core/models/blocks.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-form-block',
  imports: [FormsModule],
  templateUrl: './form-block.html',
  styleUrl: './form-block.scss',
})
export class FormBlockComponent {
  readonly block = input.required<FormBlock>();

  private readonly http = inject(HttpClient);

  protected readonly values = signal<Record<string, string>>({});
  protected readonly sending = signal(false);
  protected readonly sent = signal(false);
  protected readonly failed = signal(false);
  protected readonly touched = signal(false);

  /** Never shown to a visitor; a filled value marks the sender as a bot. */
  protected honeypot = '';

  protected readonly fields = computed(() => this.block().fields?.contentItems ?? []);

  protected options(field: FormFieldItem): string[] {
    return (field.options ?? '')
      .split('\n')
      .map((o) => o.trim())
      .filter(Boolean);
  }

  protected value(field: FormFieldItem): string {
    return this.values()[field.contentItemId] ?? '';
  }

  protected setValue(field: FormFieldItem, value: string): void {
    this.values.update((v) => ({ ...v, [field.contentItemId]: value }));
  }

  /** A required field is missing only once the visitor has tried to submit. */
  protected isMissing(field: FormFieldItem): boolean {
    return this.touched() && field.required === true && !this.value(field).trim();
  }

  protected fieldClass(field: FormFieldItem): string {
    return `field width-${field.width || 'full'}`;
  }

  protected submit(): void {
    this.touched.set(true);
    this.failed.set(false);

    const missing = this.fields().some(
      (f) => f.required === true && !this.value(f).trim(),
    );
    if (missing || this.sending()) {
      return;
    }

    this.sending.set(true);

    this.http
      .post<{ ok: boolean }>(`${environment.cmsUrl}/api/forms/submit`, {
        formName: this.block().heading || 'Form',
        // The server looks the notify address up from this block, so the
        // browser never decides who gets emailed.
        formBlockId: this.block().contentItemId,
        website: this.honeypot,
        answers: this.fields().map((f) => ({
          label: f.displayText,
          value: this.value(f),
        })),
      })
      .subscribe({
        next: () => {
          this.sending.set(false);
          this.sent.set(true);
        },
        error: () => {
          this.sending.set(false);
          this.failed.set(true);
        },
      });
  }
}

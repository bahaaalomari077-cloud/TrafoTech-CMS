import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from './i18n.service';

/**
 * Impure so it re-evaluates when the language signal flips; the translation table is a plain
 * object lookup, so the cost is negligible.
 */
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(key: string): string {
    return this.i18n.translate(key);
  }
}

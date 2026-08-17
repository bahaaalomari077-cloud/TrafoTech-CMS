import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Marks CMS-authored HTML as trusted for [innerHTML].
 *
 * This bypasses Angular's sanitizer, which is acceptable only because the HTML originates from
 * authenticated Orchard editors — never from anonymous input. If untrusted sources are ever
 * rendered through this pipe, sanitize server-side first.
 */
@Pipe({ name: 'safeHtml' })
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(html: string | null | undefined): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html ?? '');
  }
}

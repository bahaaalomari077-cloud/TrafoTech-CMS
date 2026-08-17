import { Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { ContentService } from '../../core/content.service';
import { I18nService } from '../../core/i18n.service';
import { PageWithSections } from '../../core/models/blocks.models';
import { SectionHostComponent } from '../../blocks/section-host/section-host';
import { TranslatePipe } from '../../core/translate.pipe';

/**
 * Renders any section-built Page by its autoroute path, so a page added in the
 * CMS appears on the site without a matching route being written here.
 */
@Component({
  selector: 'app-cms-page',
  imports: [SectionHostComponent, TranslatePipe, RouterLink],
  templateUrl: './cms-page.html',
})
export class CmsPageComponent {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18nService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  protected readonly page = signal<PageWithSections | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  private slug = '';

  constructor() {
    this.route.url.subscribe((segments) => {
      this.slug = segments.map((s) => s.path).join('/');
      this.load();
    });

    // Re-fetch when the language changes, so the toggle swaps the page too.
    effect(() => {
      this.i18n.lang();
      if (this.slug) {
        this.load();
      }
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(false);

    const culture = this.i18n.lang();
    // English pages live under /en. The URL already carries that prefix when
    // the visitor followed an English link, so it is only added when missing.
    // The bare "en" — the English home — counts as already prefixed: testing
    // only for "en/" prefixed it a second time and asked for "en/en".
    const slug = this.slug;
    const prefixed = slug === 'en' || slug.startsWith('en/');
    const path = culture === 'en' && !prefixed ? `en/${slug}` : slug;

    this.content.getPageWithSections(path, culture).subscribe({
      next: (page) => {
        this.page.set(page);
        this.applySeo(page);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  private applySeo(page: PageWithSections | null): void {
    if (!page) {
      return;
    }
    this.title.setTitle(page.seo?.metaTitle || page.displayText);
    if (page.seo?.metaDescription) {
      this.meta.updateTag({ name: 'description', content: page.seo.metaDescription });
    }
  }
}

import { Component, inject, signal } from '@angular/core';
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

  constructor() {
    this.route.url.subscribe((segments) => {
      this.load(segments.map((s) => s.path).join('/'));
    });
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(false);

    const culture = this.i18n.lang();
    // English pages live under /en, so the CMS path carries that prefix too.
    const path = culture === 'en' && !slug.startsWith('en/') ? `en/${slug}` : slug;

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

import { Component, effect, inject, signal } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';
import { ContentService } from '../../core/content.service';
import { I18nService } from '../../core/i18n.service';
import { PageWithSections } from '../../core/models/blocks.models';
import { SectionHostComponent } from '../../blocks/section-host/section-host';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-home',
  imports: [SectionHostComponent, TranslatePipe],
  templateUrl: './home.html',
})
export class HomeComponent {
  private readonly content = inject(ContentService);
  private readonly i18n = inject(I18nService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  protected readonly page = signal<PageWithSections | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    // Re-fetches whenever the language changes, so the toggle swaps content too.
    effect(() => {
      const lang = this.i18n.lang();
      this.load(lang);
    });
  }

  protected load(culture = this.i18n.lang()): void {
    this.loading.set(true);
    this.error.set(false);

    // Autoroute gives each culture its own path: Arabic keeps /home, English lives at /en.
    const path = culture === 'en' ? 'en' : 'home';

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
    if (page.seo?.metaKeywords) {
      this.meta.updateTag({ name: 'keywords', content: page.seo.metaKeywords });
    }
  }
}

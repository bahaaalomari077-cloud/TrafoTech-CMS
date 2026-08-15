import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContentService } from '../../core/content.service';
import { Page } from '../../core/models/content.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';
import { TranslatePipe } from '../../core/translate.pipe';

/** Renders any Orchard Page by its autoroute path, e.g. /about or /contact. */
@Component({
  selector: 'app-cms-page',
  imports: [RouterLink, TranslatePipe, SafeHtmlPipe],
  templateUrl: './cms-page.html',
})
export class CmsPageComponent {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);

  protected readonly page = signal<Page | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.route.paramMap.subscribe((params) => {
      this.load(params.get('slug') ?? '');
    });
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(false);

    this.content.getPageByPath(slug).subscribe({
      next: (page) => {
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected heroImage(page: Page | null): string | null {
    return this.content.mediaUrl(page?.heroImage?.files?.[0]?.url);
  }
}

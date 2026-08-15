import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ChromeService } from '../../core/chrome.service';
import { ContentService } from '../../core/content.service';
import { I18nService } from '../../core/i18n.service';
import { ThemeService } from '../../core/theme.service';

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './site-header.html',
  styleUrl: './site-header.scss',
})
export class SiteHeaderComponent {
  private readonly chrome = inject(ChromeService);
  private readonly content = inject(ContentService);
  protected readonly i18n = inject(I18nService);
  protected readonly theme = inject(ThemeService);

  protected readonly header = this.chrome.header;

  protected logoUrl(): string | null {
    return this.content.mediaUrl(this.header()?.logo?.files?.[0]?.url);
  }

  /** Empty until the CMS responds, so the nav renders nothing rather than a flash of defaults. */
  protected navLinks() {
    return this.header()?.navLinks?.contentItems ?? [];
  }

  protected href(url: string | null | undefined): string {
    return url || '/';
  }

  /** The switcher shows the language it switches *to*. */
  protected otherLangLabel(): string {
    return this.i18n.lang() === 'ar' ? 'EN' : 'ع';
  }
}

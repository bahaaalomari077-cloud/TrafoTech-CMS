import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
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
  private readonly router = inject(Router);

  protected readonly header = this.chrome.header;

  /**
   * The logo comes from the CMS, so an editor can change it without a rebuild.
   * The bundled file is only a fallback for before the CMS responds, or if no
   * logo has been set on the header item.
   */
  protected logoUrl(): string | null {
    return this.content.mediaUrl(this.header()?.logo?.files?.[0]?.url)
      ?? '/travotech-logo.png';
  }

  /** Empty until the CMS responds, so the nav renders nothing rather than a flash of defaults. */
  protected navLinks() {
    return this.header()?.navLinks?.contentItems ?? [];
  }

  protected href(url: string | null | undefined): string {
    return this.i18n.localizeHref(url);
  }

  /** The switcher shows the language it switches *to*. */
  protected otherLangLabel(): string {
    return this.i18n.lang() === 'ar' ? 'EN' : 'ع';
  }

  /**
   * Switches language by navigating, not by flipping a flag in place.
   *
   * The language is expressed by the URL — English pages live under /en — so
   * changing it without moving would leave the address contradicting the page,
   * and sharing that address would land the reader on the wrong version.
   */
  protected switchLang(): void {
    const path = this.router.url.split('?')[0].split('#')[0];
    const isEnglish = path === '/en' || path.startsWith('/en/');
    const target = isEnglish ? path.slice(3) || '/' : `/en${path === '/' ? '' : path}`;

    // Record the choice first: the root serves either language, and this is the
    // preference it reads on a later visit.
    this.i18n.setLang(isEnglish ? 'ar' : 'en');
    this.router.navigateByUrl(target);
  }
}

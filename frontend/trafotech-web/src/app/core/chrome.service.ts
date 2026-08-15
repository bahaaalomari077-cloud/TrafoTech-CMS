import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { GraphQLClient } from './graphql.client';
import { I18nService } from './i18n.service';
import { SiteFooter, SiteHeader } from './models/chrome.models';

const HEADER_FIELDS = `
  brandName
  logo { files { url } }
  ctaText
  ctaLink { url text }
  showLanguageSwitcher
  showThemeToggle
  sticky
  navLinks { contentItems { ... on NavLink { contentItemId displayText link { url text } } } }
`;

const FOOTER_FIELDS = `
  brandName
  logo { files { url } }
  tagline
  email
  phone
  location
  copyright
  complianceNote
  linkColumns {
    contentItems {
      ... on FooterLinkColumn {
        contentItemId
        displayText
        links { contentItems { ... on FooterLink { contentItemId displayText link { url text } } } }
      }
    }
  }
`;

/**
 * Site chrome comes from the CMS, so the header and footer carry no hardcoded
 * copy. Both are fetched per culture with a fallback to any culture, and the
 * result is cached in signals — the shell renders on every route, so refetching
 * per navigation would be wasteful.
 */
@Injectable({ providedIn: 'root' })
export class ChromeService {
  private readonly gql = inject(GraphQLClient);
  private readonly i18n = inject(I18nService);

  private readonly headerSignal = signal<SiteHeader | null>(null);
  private readonly footerSignal = signal<SiteFooter | null>(null);

  readonly header = computed(() => this.headerSignal());
  readonly footer = computed(() => this.footerSignal());

  constructor() {
    effect(() => {
      const culture = this.i18n.lang();
      this.load(culture);
    });
  }

  private load(culture: string): void {
    const query = `query Chrome($culture: String) {
      header: siteHeader(where: { localization: { culture: $culture } }) { ${HEADER_FIELDS} }
      headerFallback: siteHeader { ${HEADER_FIELDS} }
      footer: siteFooter(where: { localization: { culture: $culture } }) { ${FOOTER_FIELDS} }
      footerFallback: siteFooter { ${FOOTER_FIELDS} }
    }`;

    this.gql
      .query<{
        header: SiteHeader[];
        headerFallback: SiteHeader[];
        footer: SiteFooter[];
        footerFallback: SiteFooter[];
      }>(query, { culture })
      .subscribe({
        next: (data) => {
          this.headerSignal.set(data.header?.[0] ?? data.headerFallback?.[0] ?? null);
          this.footerSignal.set(data.footer?.[0] ?? data.footerFallback?.[0] ?? null);
        },
        error: () => {
          // Leave whatever was already loaded in place; the shell degrades to
          // its minimal form rather than blanking out.
        },
      });
  }
}

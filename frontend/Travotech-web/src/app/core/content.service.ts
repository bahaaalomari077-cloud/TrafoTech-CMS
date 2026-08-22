import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { GraphQLClient } from './graphql.client';
import { Page } from './models/content.models';
import { PageWithSections } from './models/blocks.models';
import { environment } from '../../environments/environment';

/**
 * Blocks are fetched with inline fragments per type. Adding a block type means
 * adding a fragment here and a case in BlockHostComponent.
 */
/**
 * Layout is selected identically by every block type. Orchard's GraphQL
 * complexity analyzer counts each inline repetition, and repeating these
 * fields across all block fragments overruns its budget, so the selection is
 * spread from one named fragment instead.
 */
const LAYOUT_FRAGMENT = `
  fragment LayoutParts on BlockLayout {
    width
    textAlign
    verticalAlign
    padding
    card
    shadow
    radius
  }
`;

const BLOCK_FIELDS = `
  __typename
  contentItemId
  displayText
  ... on PageHeroBlock { blockLayout { ...LayoutParts } 
    eyebrow headingBefore headingHighlight headingAfter lead
    primaryText primaryLink { url text }
    secondaryText secondaryLink { url text }
    dialValue dialLabel badgeTitle badgeText
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on FeatureGridBlock { blockLayout { ...LayoutParts } 
    eyebrow heading lead
    features { contentItems { ... on FeatureItem { contentItemId displayText icon description } } }
  }
  ... on ProductGridBlock { blockLayout { ...LayoutParts } 
    eyebrow heading linkText link { url text }
    products {
      contentItems {
        ... on ProductCard {
          contentItemId displayText badge description link { url text }
          points { contentItems { contentItemId displayText } }
        }
      }
    }
  }
  ... on PageHeaderBlock { blockLayout { ...LayoutParts } eyebrow heading lead }
  ... on ProductListBlock { blockLayout { ...LayoutParts } 
    allLabel showFilters
    categories { contentItems { ... on ProductCategory { contentItemId displayText key } } }
    rows {
      contentItems {
        ... on ProductRow {
          contentItemId displayText icon category categoryLabel description link { url text }
          points { contentItems { contentItemId displayText } }
        }
      }
    }
  }
  ... on ContactFormBlock { blockLayout { ...LayoutParts } heading intro recipientEmail submitButtonText successMessage showOrganisation nameLabel emailLabel organisationLabel messageLabel }
  ... on ContactInfoBlock { blockLayout { ...LayoutParts } heading email phone address hours note emailLabel phoneLabel addressLabel hoursLabel }
  ... on StoryBlock { blockLayout { ...LayoutParts } 
    eyebrow heading body { html }
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on TimelineBlock { blockLayout { ...LayoutParts } 
    eyebrow heading
    milestones { contentItems { ... on MilestoneItem { contentItemId displayText year description } } }
  }
  ... on ValuesBlock { blockLayout { ...LayoutParts } 
    eyebrow heading
    values { contentItems { ... on ValueItem { contentItemId displayText description } } }
  }
  ... on CtaBlock { blockLayout { ...LayoutParts } heading text buttonText buttonLink { url text } }
  ... on RichTextBlock { blockLayout { ...LayoutParts } body { html } }
  ... on MediaBlock { blockLayout { ...LayoutParts } caption media { files { url mediaText } } }
  ... on PhotoGalleryBlock {
    blockLayout { ...LayoutParts }
    heading lead columns lightbox
    images { files { url mediaText } }
  }
  ... on VideoGalleryBlock {
    blockLayout { ...LayoutParts }
    heading lead columns
    videos { contentItems { ... on VideoItem {
      contentItemId displayText videoUrl description
      video { files { url } }
      poster { files { url } }
    } } }
  }
  ... on FormBlock {
    blockLayout { ...LayoutParts }
    heading intro submitText successMessage errorMessage
    fields { contentItems { ... on FormField {
      contentItemId displayText fieldType placeholder options required width
    } } }
  }
  ... on FaqBlock { blockLayout { ...LayoutParts } 
    heading
    questions { contentItems { ... on FaqItem { contentItemId displayText answer { html } } } }
  }
`;

const SECTION_FIELDS = `
  contentItemId
  displayText
  anchor
  background
  width
  spacing
  textAlign
  border
  noReveal
  revealStyle
  pattern
  layout
  splitRatio
  stickyPanel
  hidden
  blocks { contentItems { ${BLOCK_FIELDS} } }
`;

const PAGE_FIELDS = `
  contentItemId
  displayText
  path
  metaDescription
  body { html }
  heroImage { files { url mediaText } }
  localization { culture }
`;

@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly gql = inject(GraphQLClient);

  /** Resolves media paths, which GraphQL returns relative to the CMS origin. */
  mediaUrl(url: string | undefined | null): string | null {
    if (!url) {
      return null;
    }
    return /^https?:\/\//i.test(url) ? url : `${environment.cmsUrl}${url}`;
  }

  /**
   * Fetches a page with its full section/block tree, in the requested culture.
   *
   * Falls back to any culture when the requested one has no translation yet, so
   * an untranslated page still renders rather than showing an empty screen.
   */
  getPageWithSections(path: string, culture?: string): Observable<PageWithSections | null> {
    // English paths carry an "en/" prefix. If that localized version does not
    // exist, falling back to the default path lets the page render in its
    // original culture instead of showing a blank or 404.
    const fallbackPath = path.startsWith('en/') ? path.slice(3) : path;

    // Orchard's localization filter dereferences the culture, so passing null
    // throws server-side. Without a culture there is nothing to localize by:
    // query the path alone and let the fallback alias do the work.
    const localizedPart = culture
      ? `localized: page(where: { path: $path, localization: { culture: $culture } }) {
        ...PageParts
      }`
      : '';
    // $path is only referenced by the localized alias, and GraphQL rejects a
    // declared-but-unused variable, so it is declared only alongside it.
    const localizedVars = culture ? ', $path: String!, $culture: String!' : '';

    const query = `query PageSections($fallbackPath: String!${localizedVars}) {
      ${localizedPart}
      fallback: page(where: { path: $fallbackPath }) {
        ...PageParts
      }
    }
    fragment PageParts on Page {
      contentItemId
      displayText
      path
      seo { metaTitle metaDescription metaKeywords }
      sections { contentItems { ... on Section { ${SECTION_FIELDS} } } }
    }
    ${LAYOUT_FRAGMENT}`;

    return this.gql
      .query<{ localized?: PageWithSections[]; fallback: PageWithSections[] }>(query, {
        fallbackPath,
        ...(culture ? { path, culture } : {}),
      })
      .pipe(
        map((data) => {
          const page = data.localized?.[0] ?? data.fallback?.[0];
          if (!page?.sections) {
            return page ?? null;
          }
          // Hidden sections are authored but withheld, so drop them client-side.
          return {
            ...page,
            sections: {
              contentItems: page.sections.contentItems.filter((s) => !s.hidden),
            },
          };
        }),
      );
  }

  getPageByPath(path: string): Observable<Page | null> {
    return this.gql
      .query<{ page: Page[] }>(
        `query PageByPath($path: String!) {
           page(where: { path: $path }) { ${PAGE_FIELDS} }
         }`,
        { path },
      )
      .pipe(map((data) => data.page?.[0] ?? null));
  }

}

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
const BLOCK_FIELDS = `
  __typename
  contentItemId
  displayText
  ... on PageHeroBlock { blockLayout { width } 
    eyebrow headingBefore headingHighlight headingAfter lead
    primaryText primaryLink { url text }
    secondaryText secondaryLink { url text }
    dialValue dialLabel badgeTitle badgeText
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on FeatureGridBlock { blockLayout { width } 
    eyebrow heading lead
    features { contentItems { ... on FeatureItem { contentItemId displayText icon description } } }
  }
  ... on ProductGridBlock { blockLayout { width } 
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
  ... on PageHeaderBlock { blockLayout { width } eyebrow heading lead }
  ... on ProductListBlock { blockLayout { width } 
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
  ... on ContactFormBlock { blockLayout { width } heading intro recipientEmail submitButtonText successMessage showOrganisation nameLabel emailLabel organisationLabel messageLabel }
  ... on ContactInfoBlock { blockLayout { width } heading email phone address hours note emailLabel phoneLabel addressLabel hoursLabel }
  ... on StoryBlock { blockLayout { width } 
    eyebrow heading body { html }
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on TimelineBlock { blockLayout { width } 
    eyebrow heading
    milestones { contentItems { ... on MilestoneItem { contentItemId displayText year description } } }
  }
  ... on ValuesBlock { blockLayout { width } 
    eyebrow heading
    values { contentItems { ... on ValueItem { contentItemId displayText description } } }
  }
  ... on CtaBlock { blockLayout { width } heading text buttonText buttonLink { url text } }
  ... on RichTextBlock { blockLayout { width } body { html } }
  ... on MediaBlock { blockLayout { width } caption media { files { url mediaText } } }
  ... on PhotoGalleryBlock {
    blockLayout { width }
    heading lead columns lightbox
    images { files { url mediaText } }
  }
  ... on VideoGalleryBlock {
    blockLayout { width }
    heading lead columns
    videos { contentItems { ... on VideoItem {
      contentItemId displayText videoUrl description
      video { files { url } }
      poster { files { url } }
    } } }
  }
  ... on FormBlock {
    blockLayout { width }
    heading intro submitText successMessage errorMessage
    fields { contentItems { ... on FormField {
      contentItemId displayText fieldType placeholder options required width
    } } }
  }
  ... on FaqBlock { blockLayout { width } 
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
    const query = `query PageSections($path: String!, $culture: String) {
      localized: page(where: { path: $path, localization: { culture: $culture } }) {
        ...PageParts
      }
      fallback: page(where: { path: $path }) {
        ...PageParts
      }
    }
    fragment PageParts on Page {
      contentItemId
      displayText
      path
      seo { metaTitle metaDescription metaKeywords }
      sections { contentItems { ... on Section { ${SECTION_FIELDS} } } }
    }`;

    return this.gql
      .query<{ localized: PageWithSections[]; fallback: PageWithSections[] }>(query, {
        path,
        culture: culture ?? null,
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

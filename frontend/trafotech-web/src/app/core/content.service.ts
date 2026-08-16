import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { GraphQLClient } from './graphql.client';
import { BlogPost, Page, Service } from './models/content.models';
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
  ... on PageHeroBlock {
    eyebrow headingBefore headingHighlight headingAfter lead
    primaryText primaryLink { url text }
    secondaryText secondaryLink { url text }
    dialValue dialLabel badgeTitle badgeText
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on FeatureGridBlock {
    eyebrow heading lead
    features { contentItems { ... on FeatureItem { contentItemId displayText icon description } } }
  }
  ... on ProductGridBlock {
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
  ... on PageHeaderBlock { eyebrow heading lead }
  ... on ProductListBlock {
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
  ... on ContactFormBlock { heading intro recipientEmail submitButtonText successMessage showOrganisation }
  ... on ContactInfoBlock { heading email phone address hours note }
  ... on StoryBlock {
    eyebrow heading body { html }
    stats { contentItems { ... on StatItem { contentItemId displayText value suffix label } } }
  }
  ... on TimelineBlock {
    eyebrow heading
    milestones { contentItems { ... on MilestoneItem { contentItemId displayText year description } } }
  }
  ... on ValuesBlock {
    eyebrow heading
    values { contentItems { ... on ValueItem { contentItemId displayText description } } }
  }
  ... on CtaBlock { heading text buttonText buttonLink { url text } }
  ... on RichTextBlock { body { html } }
  ... on MediaBlock { caption media { files { url mediaText } } }
  ... on FaqBlock {
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

const SERVICE_FIELDS = `
  contentItemId
  displayText
  path
  summary
  order
  description { html }
  icon { files { url mediaText } }
  localization { culture }
`;

const BLOG_POST_FIELDS = `
  contentItemId
  displayText
  path
  subtitle
  metaDescription
  publishedOn
  createdUtc
  coverImage { files { url mediaText } }
  markdownBody { html }
  categories { termContentItems { contentItemId displayText } }
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

  getServices(): Observable<Service[]> {
    return this.gql
      .query<{ service: Service[] }>(`query Services { service { ${SERVICE_FIELDS} } }`)
      .pipe(
        // Orchard's orderBy only covers built-in metadata, so the editorial order is applied here.
        map((data) =>
          [...(data.service ?? [])].sort((a, b) => (a.order ?? 0) - (b.order ?? 0)),
        ),
      );
  }

  getServiceByPath(path: string): Observable<Service | null> {
    return this.gql
      .query<{ service: Service[] }>(
        `query ServiceByPath($path: String!) {
           service(where: { path: $path }) { ${SERVICE_FIELDS} }
         }`,
        { path },
      )
      .pipe(map((data) => data.service?.[0] ?? null));
  }

  getBlogPosts(first = 20, skip = 0): Observable<BlogPost[]> {
    return this.gql
      .query<{ blogPost: BlogPost[] }>(
        `query BlogPosts($first: Int, $skip: Int) {
           blogPost(first: $first, skip: $skip, orderBy: { createdUtc: DESC }) {
             ${BLOG_POST_FIELDS}
           }
         }`,
        { first, skip },
      )
      .pipe(map((data) => data.blogPost ?? []));
  }

  getBlogPostByPath(path: string): Observable<BlogPost | null> {
    return this.gql
      .query<{ blogPost: BlogPost[] }>(
        `query BlogPostByPath($path: String!) {
           blogPost(where: { path: $path }) { ${BLOG_POST_FIELDS} }
         }`,
        { path },
      )
      .pipe(map((data) => data.blogPost?.[0] ?? null));
  }
}

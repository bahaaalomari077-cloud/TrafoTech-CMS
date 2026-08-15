import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { GraphQLClient } from './graphql.client';
import { BlogPost, Page, Service } from './models/content.models';
import { environment } from '../../environments/environment';

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

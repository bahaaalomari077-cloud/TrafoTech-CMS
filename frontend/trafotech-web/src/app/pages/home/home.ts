import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ContentService } from '../../core/content.service';
import { BlogPost, Page, Service } from '../../core/models/content.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-home',
  imports: [RouterLink, TranslatePipe, SafeHtmlPipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomeComponent {
  private readonly content = inject(ContentService);

  protected readonly page = signal<Page | null>(null);
  protected readonly services = signal<Service[]>([]);
  protected readonly posts = signal<BlogPost[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      page: this.content.getPageByPath('home'),
      services: this.content.getServices(),
      posts: this.content.getBlogPosts(3),
    }).subscribe({
      next: ({ page, services, posts }) => {
        this.page.set(page);
        this.services.set(services.slice(0, 6));
        this.posts.set(posts);
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

  /** Autoroute paths are absolute from the CMS root; the SPA routes mirror them 1:1. */
  protected routeFor(path: string): string {
    return `/${path}`;
  }
}

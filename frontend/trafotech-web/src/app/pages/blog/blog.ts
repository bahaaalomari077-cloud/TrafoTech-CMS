import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContentService } from '../../core/content.service';
import { I18nService } from '../../core/i18n.service';
import { BlogPost } from '../../core/models/content.models';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-blog',
  imports: [RouterLink, DatePipe, TranslatePipe],
  templateUrl: './blog.html',
})
export class BlogComponent {
  private readonly content = inject(ContentService);
  protected readonly i18n = inject(I18nService);

  protected readonly posts = signal<BlogPost[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);

    this.content.getBlogPosts(50).subscribe({
      next: (posts) => {
        this.posts.set(posts);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected coverUrl(post: BlogPost): string | null {
    return this.content.mediaUrl(post.coverImage?.files?.[0]?.url);
  }

  /** Falls back to the CMS creation date when an editor left PublishedOn empty. */
  protected dateFor(post: BlogPost): string | null {
    return post.publishedOn ?? post.createdUtc ?? null;
  }

  protected routeFor(path: string): string {
    return `/${path}`;
  }
}

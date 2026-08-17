import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContentService } from '../../core/content.service';
import { I18nService } from '../../core/i18n.service';
import { BlogPost } from '../../core/models/content.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-blog-detail',
  imports: [RouterLink, DatePipe, TranslatePipe, SafeHtmlPipe],
  templateUrl: './blog-detail.html',
})
export class BlogDetailComponent {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  protected readonly i18n = inject(I18nService);

  protected readonly post = signal<BlogPost | null>(null);
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

    // Mirrors the CMS autoroute pattern for blog posts.
    this.content.getBlogPostByPath(`blog/${slug}`).subscribe({
      next: (post) => {
        this.post.set(post);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected coverUrl(post: BlogPost | null): string | null {
    return this.content.mediaUrl(post?.coverImage?.files?.[0]?.url);
  }

  protected dateFor(post: BlogPost | null): string | null {
    return post?.publishedOn ?? post?.createdUtc ?? null;
  }
}

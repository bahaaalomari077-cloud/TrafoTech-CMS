import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChromeService } from '../../core/chrome.service';
import { ContentService } from '../../core/content.service';

@Component({
  selector: 'app-site-footer',
  imports: [RouterLink],
  templateUrl: './site-footer.html',
  styleUrl: './site-footer.scss',
})
export class SiteFooterComponent {
  private readonly chrome = inject(ChromeService);
  private readonly content = inject(ContentService);

  protected readonly footer = this.chrome.footer;

  protected logoUrl(): string | null {
    return this.content.mediaUrl(this.footer()?.logo?.files?.[0]?.url);
  }

  protected columns() {
    return this.footer()?.linkColumns?.contentItems ?? [];
  }

  /** mailto:/tel: links must bypass the router, which only handles in-app paths. */
  protected isExternal(url: string | null | undefined): boolean {
    return !!url && /^(mailto:|tel:|https?:)/i.test(url);
  }

  protected href(url: string | null | undefined): string {
    return url || '/';
  }
}

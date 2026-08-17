import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContentService } from '../../core/content.service';
import { Service } from '../../core/models/content.models';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-service-detail',
  imports: [RouterLink, TranslatePipe, SafeHtmlPipe],
  templateUrl: './service-detail.html',
})
export class ServiceDetailComponent {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);

  protected readonly service = signal<Service | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    // paramMap rather than a snapshot, so navigating between sibling services refetches.
    this.route.paramMap.subscribe((params) => {
      this.load(params.get('slug') ?? '');
    });
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(false);

    // The CMS autoroute pattern prefixes services, so the full path is reconstructed here.
    this.content.getServiceByPath(`services/${slug}`).subscribe({
      next: (service) => {
        this.service.set(service);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected iconUrl(service: Service | null): string | null {
    return this.content.mediaUrl(service?.icon?.files?.[0]?.url);
  }
}

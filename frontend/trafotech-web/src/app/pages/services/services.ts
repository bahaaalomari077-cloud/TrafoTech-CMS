import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ContentService } from '../../core/content.service';
import { Service } from '../../core/models/content.models';
import { TranslatePipe } from '../../core/translate.pipe';

@Component({
  selector: 'app-services',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './services.html',
})
export class ServicesComponent {
  private readonly content = inject(ContentService);

  protected readonly services = signal<Service[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal(false);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(false);

    this.content.getServices().subscribe({
      next: (services) => {
        this.services.set(services);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  protected iconUrl(service: Service): string | null {
    return this.content.mediaUrl(service.icon?.files?.[0]?.url);
  }

  protected routeFor(path: string): string {
    return `/${path}`;
  }
}

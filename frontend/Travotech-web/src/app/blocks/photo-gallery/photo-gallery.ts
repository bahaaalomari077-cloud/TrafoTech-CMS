import { Component, computed, inject, input, signal } from '@angular/core';
import { ContentService } from '../../core/content.service';
import { PhotoGalleryBlock } from '../../core/models/blocks.models';
import { AutofocusDirective } from '../../core/autofocus.directive';
import { BodyPortalDirective } from '../../core/body-portal.directive';

@Component({
  selector: 'app-photo-gallery',
  imports: [AutofocusDirective, BodyPortalDirective],
  templateUrl: './photo-gallery.html',
  styleUrl: './photo-gallery.scss',
})
export class PhotoGalleryComponent {
  readonly block = input.required<PhotoGalleryBlock>();

  private readonly content = inject(ContentService);

  /** Index of the photo shown full size, or null when the lightbox is closed. */
  protected readonly openIndex = signal<number | null>(null);

  protected readonly photos = computed(() =>
    (this.block().images?.files ?? []).map((f) => ({
      url: this.content.mediaUrl(f.url) ?? f.url,
      /** The media text doubles as the caption, so alt text is never empty. */
      caption: f.mediaText ?? '',
    })),
  );

  protected gridClass(): string {
    const columns = this.block().columns || 'auto';
    return `photo-grid cols-${columns}`;
  }

  protected open(index: number): void {
    if (this.block().lightbox !== false) {
      this.openIndex.set(index);
    }
  }

  protected close(): void {
    this.openIndex.set(null);
  }

  /** Wraps around, so paging never lands on a blank slide. */
  protected step(delta: number): void {
    const current = this.openIndex();
    const total = this.photos().length;
    if (current === null || total === 0) {
      return;
    }

    this.openIndex.set((current + delta + total) % total);
  }
}

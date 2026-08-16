import { Component, computed, input } from '@angular/core';
import { MediaBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-media-block',
  templateUrl: './media-block.html',
  styleUrl: './media-block.scss',
})
export class MediaBlockComponent {
  readonly block = input.required<MediaBlock>();

  /** The field allows only one file, so the block renders the first or nothing. */
  protected readonly file = computed(() => this.block().media?.files?.[0] ?? null);

  /** Alt text falls back to the caption, then to empty for a decorative image. */
  protected readonly alt = computed(
    () => this.file()?.mediaText || this.block().caption || '',
  );

  protected readonly isVideo = computed(() =>
    /\.(mp4|webm|ogg)(\?|$)/i.test(this.file()?.url ?? ''),
  );
}

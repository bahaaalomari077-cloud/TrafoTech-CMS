import { Component, computed, inject, input } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ContentService } from '../../core/content.service';
import { VideoGalleryBlock } from '../../core/models/blocks.models';

interface VideoEntry {
  title: string;
  description: string;
  /** Set when the editor uploaded a file; played by the native player. */
  fileUrl: string | null;
  /** Set when the editor gave a YouTube or Vimeo address instead. */
  embedUrl: SafeResourceUrl | null;
  posterUrl: string | null;
}

@Component({
  selector: 'app-video-gallery',
  templateUrl: './video-gallery.html',
  styleUrl: './video-gallery.scss',
})
export class VideoGalleryComponent {
  readonly block = input.required<VideoGalleryBlock>();

  private readonly content = inject(ContentService);
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly videos = computed<VideoEntry[]>(() =>
    (this.block().videos?.contentItems ?? []).map((v) => {
      const fileUrl = this.content.mediaUrl(v.video?.files?.[0]?.url) ?? null;
      const embed = fileUrl ? null : this.toEmbed(v.videoUrl);

      return {
        title: v.displayText ?? '',
        description: v.description ?? '',
        fileUrl,
        embedUrl: embed ? this.sanitizer.bypassSecurityTrustResourceUrl(embed) : null,
        posterUrl: this.content.mediaUrl(v.poster?.files?.[0]?.url) ?? null,
      };
    }),
  );

  protected gridClass(): string {
    const columns = this.block().columns || 'auto';
    return `video-grid cols-${columns}`;
  }

  /**
   * Converts a watch link into its embeddable form.
   *
   * Editors paste the address from the browser bar, which does not play in an
   * iframe. Anything unrecognised returns null so the entry renders as a plain
   * link rather than a blank frame.
   */
  private toEmbed(url: string | null | undefined): string | null {
    if (!url) {
      return null;
    }

    const youtube = url.match(
      /(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([\w-]{11})/,
    );
    if (youtube) {
      return `https://www.youtube.com/embed/${youtube[1]}`;
    }

    const vimeo = url.match(/vimeo\.com\/(?:video\/)?(\d+)/);
    if (vimeo) {
      return `https://player.vimeo.com/video/${vimeo[1]}`;
    }

    return null;
  }
}

import { Component, input } from '@angular/core';
import { FeatureGridBlock } from '../../core/models/blocks.models';

@Component({
  selector: 'app-feature-grid',
  templateUrl: './feature-grid.html',
  styleUrl: './feature-grid.scss',
})
export class FeatureGridComponent {
  readonly block = input.required<FeatureGridBlock>();

  /** Inline glyphs keep the block self-contained — no icon font or sprite to load. */
  protected glyph(icon: string | null | undefined): string {
    switch (icon) {
      case 'growth':
        return 'M3 17l6-6 4 4 8-8';
      case 'link':
        return 'M10 13a5 5 0 007 0l3-3a5 5 0 00-7-7l-1 1M14 11a5 5 0 00-7 0l-3 3a5 5 0 007 7l1-1';
      case 'support':
        return 'M12 22a10 10 0 100-20 10 10 0 000 20zM12 16v-4M12 8h.01';
      case 'bolt':
        return 'M13 2L4 14h7l-1 8 9-12h-7l1-8z';
      case 'clock':
        return 'M12 22a10 10 0 100-20 10 10 0 000 20zM12 6v6l4 2';
      default:
        return 'M12 2l8 4v6c0 5-3.4 8.6-8 10-4.6-1.4-8-5-8-10V6l8-4z';
    }
  }
}

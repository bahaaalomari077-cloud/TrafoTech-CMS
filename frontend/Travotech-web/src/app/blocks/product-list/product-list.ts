import { Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductListBlock, ProductRow } from '../../core/models/blocks.models';

@Component({
  selector: 'app-product-list',
  imports: [RouterLink],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductListComponent {
  readonly block = input.required<ProductListBlock>();

  /** Empty string means "all"; filtering is client-side over an already-loaded list. */
  protected readonly activeCategory = signal('');

  protected readonly categories = computed(() => this.block().categories?.contentItems ?? []);

  protected readonly rows = computed<ProductRow[]>(() => {
    const all = this.block().rows?.contentItems ?? [];
    const active = this.activeCategory();
    return active ? all.filter((r) => r.category === active) : all;
  });

  protected select(key: string): void {
    this.activeCategory.set(key);
  }

  protected glyph(icon: string | null | undefined): string {
    switch (icon) {
      case 'bolt':
        return 'M13 2L4 14h7l-1 8 9-12h-7l1-8z';
      case 'wallet':
        return 'M3 7h16a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7zm0 0V6a2 2 0 012-2h11M17 13h.01';
      case 'check':
        return 'M20 6L9 17l-5-5';
      case 'hub':
        return 'M12 12m-3 0a3 3 0 106 0 3 3 0 10-6 0M12 2v7M12 15v7M2 12h7M15 12h7';
      case 'card':
        return 'M2 7h20v10a2 2 0 01-2 2H4a2 2 0 01-2-2V7zm0 4h20M6 15h4';
      case 'shield':
        return 'M12 2l8 4v6c0 5-3.4 8.6-8 10-4.6-1.4-8-5-8-10V6l8-4z';
      case 'growth':
        return 'M3 17l6-6 4 4 8-8';
      default:
        return 'M3 12h4l3 8 4-16 3 8h4';
    }
  }

  protected href(url: string | null | undefined): string {
    return url || '/';
  }
}

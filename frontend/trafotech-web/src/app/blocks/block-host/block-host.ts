import { Component, input } from '@angular/core';
import {
  Block,
  CtaBlock,
  FeatureGridBlock,
  PageHeroBlock,
  ProductGridBlock,
  RichTextBlock,
} from '../../core/models/blocks.models';
import { HeroBlockComponent } from '../hero-block/hero-block';
import { FeatureGridComponent } from '../feature-grid/feature-grid';
import { ProductGridComponent } from '../product-grid/product-grid';
import { CtaBlockComponent } from '../cta-block/cta-block';
import { SafeHtmlPipe } from '../../core/safe-html.pipe';

/**
 * Renders one block by its __typename. An unrecognised block renders nothing
 * rather than breaking the page, so adding a type in the CMS ahead of the
 * frontend is safe.
 */
@Component({
  selector: 'app-block-host',
  imports: [
    HeroBlockComponent,
    FeatureGridComponent,
    ProductGridComponent,
    CtaBlockComponent,
    SafeHtmlPipe,
  ],
  template: `
    @switch (block().__typename) {
      @case ('PageHeroBlock') {
        <app-hero-block [block]="asHero(block())" />
      }
      @case ('FeatureGridBlock') {
        <app-feature-grid [block]="asFeatures(block())" />
      }
      @case ('ProductGridBlock') {
        <app-product-grid [block]="asProducts(block())" />
      }
      @case ('CtaBlock') {
        <app-cta-block [block]="asCta(block())" />
      }
      @case ('RichTextBlock') {
        <div class="rich-text" [innerHTML]="asRichText(block()).body?.html | safeHtml"></div>
      }
    }
  `,
})
export class BlockHostComponent {
  readonly block = input.required<Block>();

  // Narrowing helpers: the template cannot assert types on a union directly.
  protected asHero(b: Block): PageHeroBlock {
    return b as PageHeroBlock;
  }
  protected asFeatures(b: Block): FeatureGridBlock {
    return b as FeatureGridBlock;
  }
  protected asProducts(b: Block): ProductGridBlock {
    return b as ProductGridBlock;
  }
  protected asCta(b: Block): CtaBlock {
    return b as CtaBlock;
  }
  protected asRichText(b: Block): RichTextBlock {
    return b as RichTextBlock;
  }
}

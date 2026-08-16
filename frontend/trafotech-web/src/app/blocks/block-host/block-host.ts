import { Component, input } from '@angular/core';
import {
  Block,
  CtaBlock,
  FeatureGridBlock,
  PageHeaderBlock,
  PageHeroBlock,
  ProductGridBlock,
  ProductListBlock,
  ContactFormBlock,
  ContactInfoBlock,
  StoryBlock,
  TimelineBlock,
  ValuesBlock,
  RichTextBlock,
} from '../../core/models/blocks.models';
import { HeroBlockComponent } from '../hero-block/hero-block';
import { FeatureGridComponent } from '../feature-grid/feature-grid';
import { ProductGridComponent } from '../product-grid/product-grid';
import { PageHeaderComponent } from '../page-header/page-header';
import { ProductListComponent } from '../product-list/product-list';
import { ContactFormComponent } from '../contact-form/contact-form';
import { ContactInfoComponent } from '../contact-info/contact-info';
import { StoryBlockComponent } from '../story-block/story-block';
import { TimelineBlockComponent } from '../timeline-block/timeline-block';
import { ValuesBlockComponent } from '../values-block/values-block';
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
    PageHeaderComponent,
    ProductListComponent,
    ContactFormComponent,
    ContactInfoComponent,
    StoryBlockComponent,
    TimelineBlockComponent,
    ValuesBlockComponent,
    CtaBlockComponent,
    SafeHtmlPipe,
  ],
  templateUrl: './block-host.html',
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
  protected asPageHeader(b: Block): PageHeaderBlock {
    return b as PageHeaderBlock;
  }
  protected asProductList(b: Block): ProductListBlock {
    return b as ProductListBlock;
  }
  protected asContactForm(b: Block): ContactFormBlock {
    return b as ContactFormBlock;
  }
  protected asContactInfo(b: Block): ContactInfoBlock {
    return b as ContactInfoBlock;
  }
  protected asStory(b: Block): StoryBlock {
    return b as StoryBlock;
  }
  protected asTimeline(b: Block): TimelineBlock {
    return b as TimelineBlock;
  }
  protected asValues(b: Block): ValuesBlock {
    return b as ValuesBlock;
  }
  protected asCta(b: Block): CtaBlock {
    return b as CtaBlock;
  }
  protected asRichText(b: Block): RichTextBlock {
    return b as RichTextBlock;
  }
}

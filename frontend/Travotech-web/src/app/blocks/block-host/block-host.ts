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
  MediaBlock,
  FaqBlock,
  PhotoGalleryBlock,
  VideoGalleryBlock,
  FormBlock,
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
import { MediaBlockComponent } from '../media-block/media-block';
import { FaqBlockComponent } from '../faq-block/faq-block';
import { PhotoGalleryComponent } from '../photo-gallery/photo-gallery';
import { VideoGalleryComponent } from '../video-gallery/video-gallery';
import { FormBlockComponent } from '../form-block/form-block';
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
    MediaBlockComponent,
    FaqBlockComponent,
    PhotoGalleryComponent,
    VideoGalleryComponent,
    FormBlockComponent,
    SafeHtmlPipe,
  ],
  templateUrl: './block-host.html',
  styleUrl: './block-host.scss',
})
export class BlockHostComponent {
  readonly block = input.required<Block>();

  protected styleClass(): string {
    const l = this.block().blockLayout;
    const classes: string[] = [];
    if (l?.textAlign) {
      classes.push(`text-${l.textAlign}`);
    }
    if (l?.padding) {
      classes.push(`pad-${l.padding}`);
    }
    if (l?.card && l.card !== 'none') {
      classes.push(`card-${l.card}`);
    }
    if (l?.shadow && l.shadow !== 'none') {
      classes.push(`shadow-${l.shadow}`);
    }
    if (l?.radius && l.radius !== 'none') {
      classes.push(`radius-${l.radius}`);
    }
    return classes.join(' ');
  }

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
  protected asMedia(b: Block): MediaBlock {
    return b as MediaBlock;
  }
  protected asFaq(b: Block): FaqBlock {
    return b as FaqBlock;
  }
  protected asPhotoGallery(b: Block): PhotoGalleryBlock {
    return b as PhotoGalleryBlock;
  }
  protected asVideoGallery(b: Block): VideoGalleryBlock {
    return b as VideoGalleryBlock;
  }
  protected asForm(b: Block): FormBlock {
    return b as FormBlock;
  }
}

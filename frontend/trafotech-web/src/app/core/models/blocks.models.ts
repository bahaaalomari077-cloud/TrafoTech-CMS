/**
 * Block shapes as returned by the CMS. Custom text fields come back as plain
 * scalars; Html/Media/Link fields stay objects, and bags nest under
 * `{ contentItems: [...] }`.
 */

export interface LinkValue {
  url?: string | null;
  text?: string | null;
}

export interface StatItem {
  contentItemId: string;
  displayText: string;
  value?: string | null;
  suffix?: string | null;
  label?: string | null;
}

export interface FeatureItem {
  contentItemId: string;
  displayText: string;
  icon?: string | null;
  description?: string | null;
}

export interface ProductPoint {
  contentItemId: string;
  displayText: string;
}

export interface ProductCard {
  contentItemId: string;
  displayText: string;
  badge?: string | null;
  description?: string | null;
  link?: LinkValue | null;
  points?: { contentItems: ProductPoint[] } | null;
}

/** Discriminated by __typename so the renderer can switch on block kind. */
export interface BlockBase {
  __typename: string;
  contentItemId: string;
  displayText?: string;
}

export interface PageHeroBlock extends BlockBase {
  __typename: 'PageHeroBlock';
  eyebrow?: string | null;
  headingBefore?: string | null;
  headingHighlight?: string | null;
  headingAfter?: string | null;
  lead?: string | null;
  primaryText?: string | null;
  primaryLink?: LinkValue | null;
  secondaryText?: string | null;
  secondaryLink?: LinkValue | null;
  dialValue?: string | null;
  dialLabel?: string | null;
  badgeTitle?: string | null;
  badgeText?: string | null;
  stats?: { contentItems: StatItem[] } | null;
}

export interface FeatureGridBlock extends BlockBase {
  __typename: 'FeatureGridBlock';
  eyebrow?: string | null;
  heading?: string | null;
  lead?: string | null;
  features?: { contentItems: FeatureItem[] } | null;
}

export interface ProductGridBlock extends BlockBase {
  __typename: 'ProductGridBlock';
  eyebrow?: string | null;
  heading?: string | null;
  linkText?: string | null;
  link?: LinkValue | null;
  products?: { contentItems: ProductCard[] } | null;
}

export interface CtaBlock extends BlockBase {
  __typename: 'CtaBlock';
  heading?: string | null;
  text?: string | null;
  buttonText?: string | null;
  buttonLink?: LinkValue | null;
}

export interface RichTextBlock extends BlockBase {
  __typename: 'RichTextBlock';
  body?: { html: string } | null;
}

export type Block =
  | PageHeroBlock
  | FeatureGridBlock
  | ProductGridBlock
  | CtaBlock
  | RichTextBlock
  | BlockBase;

export type SectionBackground = 'default' | 'alt' | 'brand' | 'dark';
export type SectionWidth = 'container' | 'wide' | 'full';
export type SectionSpacing = 'none' | 'compact' | 'normal' | 'spacious';

export interface Section {
  contentItemId: string;
  displayText: string;
  anchor?: string | null;
  background?: SectionBackground | null;
  width?: SectionWidth | null;
  spacing?: SectionSpacing | null;
  hidden?: boolean | null;
  blocks?: { contentItems: Block[] } | null;
}

export interface PageWithSections {
  contentItemId: string;
  displayText: string;
  path: string;
  seo?: {
    metaTitle?: string | null;
    metaDescription?: string | null;
    metaKeywords?: string | null;
  } | null;
  sections?: { contentItems: Section[] } | null;
}

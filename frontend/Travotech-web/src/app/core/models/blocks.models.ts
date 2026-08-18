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
export type BlockWidth = 'full' | 'half' | 'third' | 'two-thirds';

export interface BlockBase {
  __typename: string;
  contentItemId: string;
  displayText?: string;
  /** How much of the row the editor gave this block; defaults to full. */
  blockLayout?: { width?: BlockWidth | null } | null;
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

export interface ContactFormBlock extends BlockBase {
  __typename: 'ContactFormBlock';
  heading?: string | null;
  intro?: string | null;
  recipientEmail?: string | null;
  submitButtonText?: string | null;
  successMessage?: string | null;
  showOrganisation?: boolean | null;
  /* Field captions; blank falls back to the built-in wording. */
  nameLabel?: string | null;
  emailLabel?: string | null;
  organisationLabel?: string | null;
  messageLabel?: string | null;
}

export interface ContactInfoBlock extends BlockBase {
  __typename: 'ContactInfoBlock';
  heading?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  hours?: string | null;
  note?: string | null;
  /* Row headings; blank falls back to the built-in wording. */
  emailLabel?: string | null;
  phoneLabel?: string | null;
  addressLabel?: string | null;
  hoursLabel?: string | null;
}

export interface MilestoneItem {
  contentItemId: string;
  displayText: string;
  year?: string | null;
  description?: string | null;
}

export interface ValueItem {
  contentItemId: string;
  displayText: string;
  description?: string | null;
}

export interface StoryBlock extends BlockBase {
  __typename: 'StoryBlock';
  eyebrow?: string | null;
  heading?: string | null;
  body?: { html: string } | null;
  stats?: { contentItems: StatItem[] } | null;
}

export interface TimelineBlock extends BlockBase {
  __typename: 'TimelineBlock';
  eyebrow?: string | null;
  heading?: string | null;
  milestones?: { contentItems: MilestoneItem[] } | null;
}

export interface ValuesBlock extends BlockBase {
  __typename: 'ValuesBlock';
  eyebrow?: string | null;
  heading?: string | null;
  values?: { contentItems: ValueItem[] } | null;
}

export interface PageHeaderBlock extends BlockBase {
  __typename: 'PageHeaderBlock';
  eyebrow?: string | null;
  heading?: string | null;
  lead?: string | null;
}

export interface ProductCategoryItem {
  contentItemId: string;
  displayText: string;
  key?: string | null;
}

export interface ProductRow {
  contentItemId: string;
  displayText: string;
  icon?: string | null;
  category?: string | null;
  categoryLabel?: string | null;
  description?: string | null;
  link?: LinkValue | null;
  points?: { contentItems: ProductPoint[] } | null;
}

export interface ProductListBlock extends BlockBase {
  __typename: 'ProductListBlock';
  allLabel?: string | null;
  showFilters?: boolean | null;
  categories?: { contentItems: ProductCategoryItem[] } | null;
  rows?: { contentItems: ProductRow[] } | null;
}

export interface MediaFile {
  url: string;
  mediaText?: string | null;
}

export interface MediaBlock extends BlockBase {
  __typename: 'MediaBlock';
  caption?: string | null;
  media?: { files: MediaFile[] } | null;
}

export interface FaqItem {
  contentItemId: string;
  /* The question itself is the item's title; only the answer is a field. */
  displayText: string;
  answer?: { html: string } | null;
}

export interface FaqBlock extends BlockBase {
  __typename: 'FaqBlock';
  heading?: string | null;
  questions?: { contentItems: FaqItem[] } | null;
}

export type Block =
  | StoryBlock
  | TimelineBlock
  | ValuesBlock
  | ContactInfoBlock
  | PageHeroBlock
  | FeatureGridBlock
  | ProductGridBlock
  | PageHeaderBlock
  | ProductListBlock
  | CtaBlock
  | RichTextBlock
  | MediaBlock
  | FaqBlock
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

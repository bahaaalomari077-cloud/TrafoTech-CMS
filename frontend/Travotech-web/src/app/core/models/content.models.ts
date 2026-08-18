/**
 * Shapes mirror the Orchard Core GraphQL schema exactly as introspected: custom text fields are
 * flattened to scalars (`summary`, `subtitle`), while Html/Media/Taxonomy fields stay as objects.
 */

export interface MediaFile {
  url: string;
  fileName?: string;
  mediaText?: string;
}

export interface MediaField {
  files: MediaFile[];
}

export interface HtmlField {
  html: string;
}

export interface LocalizationInfo {
  culture: string;
  localizationSet?: string;
}

export interface ContentItemBase {
  contentItemId: string;
  displayText: string;
  path: string;
  localization?: LocalizationInfo | null;
}

export interface Page extends ContentItemBase {
  metaDescription?: string | null;
  body?: HtmlField | null;
  heroImage?: MediaField | null;
}

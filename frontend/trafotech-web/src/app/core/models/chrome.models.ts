import { LinkValue } from './blocks.models';

export interface NavLink {
  contentItemId: string;
  displayText: string;
  link?: LinkValue | null;
}

export interface FooterLink {
  contentItemId: string;
  displayText: string;
  link?: LinkValue | null;
}

export interface FooterColumn {
  contentItemId: string;
  displayText: string;
  links?: { contentItems: FooterLink[] } | null;
}

export interface MediaField {
  files: { url: string }[];
}

export interface SiteHeader {
  brandName?: string | null;
  logo?: MediaField | null;
  ctaText?: string | null;
  ctaLink?: LinkValue | null;
  showLanguageSwitcher?: boolean | null;
  showThemeToggle?: boolean | null;
  sticky?: boolean | null;
  navLinks?: { contentItems: NavLink[] } | null;
}

export interface SiteFooter {
  brandName?: string | null;
  logo?: MediaField | null;
  tagline?: string | null;
  email?: string | null;
  phone?: string | null;
  location?: string | null;
  copyright?: string | null;
  complianceNote?: string | null;
  linkColumns?: { contentItems: FooterColumn[] } | null;
}

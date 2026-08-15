import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type Lang = 'ar' | 'en';

const STORAGE_KEY = 'trafotech.lang';

const TRANSLATIONS: Record<Lang, Record<string, string>> = {
  ar: {
    'nav.home': 'الرئيسية',
    'nav.about': 'من نحن',
    'nav.services': 'خدماتنا',
    'nav.blog': 'المدونة',
    'nav.contact': 'اتصل بنا',
    'common.readMore': 'اقرأ المزيد',
    'common.loading': 'جارٍ التحميل…',
    'common.error': 'تعذّر تحميل المحتوى.',
    'common.retry': 'إعادة المحاولة',
    'common.notFound': 'الصفحة غير موجودة.',
    'common.backHome': 'العودة للرئيسية',
    'services.title': 'خدماتنا',
    'services.empty': 'لا توجد خدمات منشورة حالياً.',
    'blog.title': 'المدونة',
    'blog.empty': 'لا توجد مقالات منشورة حالياً.',
    'blog.publishedOn': 'نُشر في',
    'footer.rights': 'جميع الحقوق محفوظة',
    'lang.switch': 'English',
  },
  en: {
    'nav.home': 'Home',
    'nav.about': 'About',
    'nav.services': 'Services',
    'nav.blog': 'Blog',
    'nav.contact': 'Contact',
    'common.readMore': 'Read more',
    'common.loading': 'Loading…',
    'common.error': 'Could not load content.',
    'common.retry': 'Retry',
    'common.notFound': 'Page not found.',
    'common.backHome': 'Back to home',
    'services.title': 'Our Services',
    'services.empty': 'No services published yet.',
    'blog.title': 'Blog',
    'blog.empty': 'No posts published yet.',
    'blog.publishedOn': 'Published on',
    'footer.rights': 'All rights reserved',
    'lang.switch': 'العربية',
  },
};

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly document = inject(DOCUMENT);

  readonly lang = signal<Lang>(this.readInitialLang());
  readonly dir = computed<'rtl' | 'ltr'>(() => (this.lang() === 'ar' ? 'rtl' : 'ltr'));

  constructor() {
    this.applyToDocument(this.lang());
  }

  setLang(lang: Lang): void {
    this.lang.set(lang);
    this.applyToDocument(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // Private-browsing modes can reject writes; language still applies for this session.
    }
  }

  toggle(): void {
    this.setLang(this.lang() === 'ar' ? 'en' : 'ar');
  }

  translate(key: string): string {
    return TRANSLATIONS[this.lang()][key] ?? key;
  }

  private applyToDocument(lang: Lang): void {
    const html = this.document.documentElement;
    html.lang = lang;
    html.dir = lang === 'ar' ? 'rtl' : 'ltr';
  }

  private readInitialLang(): Lang {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'ar' || stored === 'en') {
        return stored;
      }
    } catch {
      // Ignore and fall through to the default.
    }
    // The site's primary audience is Arabic and the homepage is authored in it,
    // so a first visit opens in Arabic. The admin UI is English regardless.
    return 'ar';
  }
}

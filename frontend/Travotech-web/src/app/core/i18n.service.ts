import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

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
    'form.name': 'الاسم',
    'form.email': 'البريد الإلكتروني',
    'form.organisation': 'الجهة (اختياري)',
    'form.message': 'رسالتك',
    'form.send': 'إرسال',
    'contact.email': 'البريد',
    'contact.phone': 'الهاتف',
    'contact.address': 'العنوان',
    'contact.hours': 'ساعات العمل',
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
    'form.name': 'Name',
    'form.email': 'Email',
    'form.organisation': 'Organisation (optional)',
    'form.message': 'Your message',
    'form.send': 'Send',
    'contact.email': 'Email',
    'contact.phone': 'Phone',
    'contact.address': 'Address',
    'contact.hours': 'Working hours',
  },
};

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);

  readonly lang = signal<Lang>(this.readInitialLang());
  readonly dir = computed<'rtl' | 'ltr'>(() => (this.lang() === 'ar' ? 'rtl' : 'ltr'));

  constructor() {
    this.applyToDocument(this.lang());

    // The app is a single page, so the initial read alone would leave the
    // language frozen at whatever the first URL said. Following navigation
    // keeps /en and the rendered language in step for in-app links too.
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((event) => {
        const path = event.urlAfterRedirects.split('?')[0].split('#')[0];

        // The root is the one ambiguous address — it serves either language
        // depending on the stored preference — so it is left alone rather than
        // being forced back to Arabic on arrival.
        if (path === '/' || path === '') {
          return;
        }

        const lang: Lang = path === '/en' || path.startsWith('/en/') ? 'en' : 'ar';
        if (lang !== this.lang()) {
          this.lang.set(lang);
          this.applyToDocument(lang);
        }
      });
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

  /**
   * The URL wins over the stored preference.
   *
   * Every English page lives under /en, so a link, a bookmark or a shared
   * address already states the language. Reading only localStorage rendered
   * /en pages with Arabic chrome — English content wrapped in a right-to-left
   * layout with Arabic labels.
   */
  private readInitialLang(): Lang {
    const path = this.document.location?.pathname ?? '';
    if (path === '/en' || path.startsWith('/en/')) {
      return 'en';
    }

    // A path outside /en is Arabic by definition, so only the ambiguous case
    // left — the site root — consults the visitor's previous choice.
    if (path === '/' || path === '') {
      try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored === 'ar' || stored === 'en') {
          return stored;
        }
      } catch {
        // Ignore and fall through to the default.
      }
    }

    // The site's primary audience is Arabic and the homepage is authored in it,
    // so a first visit opens in Arabic. The admin UI is English regardless.
    return 'ar';
  }
}

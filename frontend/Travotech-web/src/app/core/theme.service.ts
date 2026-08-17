import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'trafotech.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<Theme>(this.readInitial());
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    this.apply(this.theme());
  }

  set(theme: Theme): void {
    this.theme.set(theme);
    this.apply(theme);
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // Private browsing can reject writes; the theme still applies this session.
    }
  }

  toggle(): void {
    this.set(this.theme() === 'dark' ? 'light' : 'dark');
  }

  private apply(theme: Theme): void {
    this.document.documentElement.dataset['theme'] = theme;
  }

  private readInitial(): Theme {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'light' || stored === 'dark') {
        return stored;
      }
    } catch {
      // Fall through to the system preference.
    }
    const prefersDark =
      typeof matchMedia === 'function' && matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}

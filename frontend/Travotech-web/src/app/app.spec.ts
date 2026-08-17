import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { I18nService } from './core/i18n.service';
import { ThemeService } from './core/theme.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    expect(TestBed.createComponent(App).componentInstance).toBeTruthy();
  });

  it('should render the shell without any hardcoded chrome copy', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('app-site-header')).toBeTruthy();
    expect(compiled.querySelector('app-site-footer')).toBeTruthy();
    // Chrome copy comes from the CMS, so nothing is present before it responds.
    expect(compiled.querySelector('.site-nav a')).toBeNull();
  });

  it('should request site chrome for the active culture', async () => {
    const i18n = TestBed.inject(I18nService);
    i18n.setLang('ar');

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const http = TestBed.inject(HttpTestingController);
    const requests = http.match(() => true);
    expect(requests.length).toBeGreaterThan(0);
    expect(requests[0].request.body.variables.culture).toBe('ar');
  });

  it('should flip document direction with the language', () => {
    const i18n = TestBed.inject(I18nService);

    i18n.setLang('ar');
    expect(i18n.dir()).toBe('rtl');

    i18n.setLang('en');
    expect(i18n.dir()).toBe('ltr');
  });

  it('should toggle the theme', () => {
    const theme = TestBed.inject(ThemeService);
    const before = theme.isDark();
    theme.toggle();
    expect(theme.isDark()).toBe(!before);
  });
});
